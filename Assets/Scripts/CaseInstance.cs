using System.Collections.Generic;

/// <summary>
/// A generated, playable case for the current shift.
/// Created at runtime from data assets (blueprints, clue library, etc.).
/// </summary>
public sealed class CaseInstance
{
    /// <summary>0-based case index used internally.</summary>
    public int caseIndex;

    /// <summary>
    /// The era the traveller claims as home and is sent to (the correct era on
    /// the legacy era-pick path). A liar's real era is trueHome.era.
    /// </summary>
    public EraSO trueEra;

    /// <summary>True if this case was generated as a legendary encounter.</summary>
    public bool isLegendary;

    /// <summary>Reference to the legendary source asset (if legendary).</summary>
    public LegendarySO legendarySource;

    /// <summary>Visitor archetype (drives default timeline impacts + tags).</summary>
    public ArchetypeSO archetype;

    /// <summary>The nation the traveller claims as home (the destination; timeline impacts land here).</summary>
    public NationSO nation;

    /// <summary>Label of the claimed place, "Abbasid Baghdad (Medieval)" (claim line and Citizen Records).</summary>
    public string originLabel;

    /// <summary>Authored impact overrides from the blueprint/legendary (may be empty).</summary>
    public readonly List<TimelineImpact> authoredImpacts = new();

    /// <summary>Display name for the visitor (given name + role suffix).</summary>
    public string visitorDisplayName;

    /// <summary>The registered given name, from the claimed place's names (a liar's cover name; citizen-records lookup key).</summary>
    public string visitorGivenName;

    /// <summary>
    /// The registered date of birth, from the claimed place's birth years (what
    /// the agency has on file). A birth-date tell prints a different year on the papers.
    /// </summary>
    public string trueBirthDate;

    /// <summary>Gender from the claimed place's name list the given name came from (Unknown for legendaries and "Subject #n").</summary>
    public TravellerGender gender;

    /// <summary>The desk's opener for this traveller (interview lines, with the traveller's honorific); the transcript's first line.</summary>
    public string introLine;

    // -----------------------------
    // Investigation (accept/deny)
    // -----------------------------

    /// <summary>Nation the visitor CLAIMS to be traveling to (shown to player).</summary>
    public NationSO claimedNation;

    /// <summary>Era the visitor CLAIMS to be traveling to (shown to player).</summary>
    public EraSO claimedEra;

    /// <summary>
    /// Where the traveller really comes from: another of today's places for a
    /// liar, null for an honest traveller (whose home is the claim).
    /// </summary>
    public NationEraProfileSO trueHome;

    /// <summary>The true home's label, from today's FactTable like <see cref="originLabel"/> (empty for an honest traveller).</summary>
    public string trueHomeLabel = string.Empty;

    /// <summary>True when the traveller lied about their home (their papers or answers leak tells).</summary>
    public bool IsLiar => trueHome != null;

    /// <summary>Where the traveller really comes from, as a label (verdict and logs).</summary>
    public string HomeLabel => IsLiar ? trueHomeLabel : originLabel;

    /// <summary>True if the claimed destination is permitted by today's rules.</summary>
    public bool claimAllowedByRules = true;

    /// <summary>The traveller's claim sentence (interview.claim with the claimed place's label); the banner, the shift summary and the transcript's second line.</summary>
    public string claimLine;

    /// <summary>The traveller's answer to each question askable today, in question order (computed at generation from the same values as the papers).</summary>
    public readonly List<InterviewAnswer> answers = new();

    /// <summary>What the traveller says when asked small talk (their claimed place's or era's flavour; null when none is authored).</summary>
    public LineText smallTalk;

    /// <summary>
    /// The correct decision: accept only an honest traveller whose destination
    /// is permitted today; deny a liar or a rule-breaking destination.
    /// </summary>
    public bool ShouldAccept => VerdictRules.ShouldAccept(IsLiar, claimAllowedByRules);

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
