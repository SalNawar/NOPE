using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A generated, playable case for the current shift.
/// Created at runtime from data assets (blueprints, templates, places, premades).
/// </summary>
public sealed class CaseInstance
{
    /// <summary>0-based case index used internally.</summary>
    public int caseIndex;

    /// <summary>The traveller's kind, their blueprint's (traveller types K1): their papers and their claim line (left at the default without a blueprint).</summary>
    public TravellerKind kind;

    /// <summary>
    /// A displaced person's agency file (AgencyNumbers.Displaced, on their
    /// account stream): the Displacement No., incident, found date and
    /// certificate's Valid Until their forms and registry entry print. Null
    /// for another kind, or when the agency calendar cannot count today.
    /// </summary>
    public DisplacementFile displacement;

    /// <summary>True if this traveller is a premade character (named, drawn whole).</summary>
    public bool isLegendary;

    /// <summary>The premade's asset (null for a generated traveller).</summary>
    public LegendarySO legendarySource;

    /// <summary>Visitor archetype (drives default timeline impacts + tags).</summary>
    public ArchetypeSO archetype;

    /// <summary>Label of the claimed place, "Abbasid Baghdad (Medieval)" (claim line and Citizen Records).</summary>
    public string originLabel;

    /// <summary>The claimed place's tongue (piece 9): the traveller speaks it, never their true home's (their papers are always English; empty without a place).</summary>
    public string tongueId = string.Empty;

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

    /// <summary>Gender from the claimed place's name list the given name came from, or the premade's (Unknown for "Subject #n").</summary>
    public TravellerGender gender;

    /// <summary>The desk's opener for this traveller (interview lines, with the traveller's honorific); the transcript's first line.</summary>
    public string introLine;

    // -----------------------------
    // Investigation (accept/deny)
    // -----------------------------

    /// <summary>
    /// The nation of the claimed place: where the traveller is sent (the
    /// destination; timeline impacts land here), for every kind a displaced
    /// person's stated home. Set even when the case has no blueprint (audit
    /// R3-020 merged the duplicate `nation`).
    /// </summary>
    public NationSO claimedNation;

    /// <summary>
    /// The era of the claimed place (the correct era on the legacy era-pick
    /// path). A liar's real era is trueHome.era. Set even when the case has no
    /// blueprint (audit R3-020 merged the duplicate `trueEra`).
    /// </summary>
    public EraSO claimedEra;

    /// <summary>
    /// Where the traveller really comes from: another of today's places for a
    /// liar, null for an honest traveller (whose home is the claim).
    /// </summary>
    public NationEraProfileSO trueHome;

    /// <summary>The true home's label, from today's FactTable like <see cref="originLabel"/> (empty for an honest traveller).</summary>
    public string trueHomeLabel = string.Empty;

    /// <summary>True when the traveller lied about their home (their papers, answers or dress leak tells).</summary>
    public bool IsLiar => trueHome != null;

    /// <summary>Where the traveller really comes from, as a label (verdict and logs).</summary>
    public string HomeLabel => IsLiar ? trueHomeLabel : originLabel;

    /// <summary>
    /// A 2150 citizen's costume error (traveller types C2): the wrong item
    /// they wear to the destination, where it would cause a panic (None for
    /// everyone else). A deviation fault, never beside another fault (K5),
    /// proven by comparing a garment with the Costume Guide.
    /// </summary>
    public CostumeError costumeFault;

    /// <summary>The label of the first wrong garment a costume error shows (the panic news names it); empty without one.</summary>
    public string CostumeItem =>
        costumeFault == CostumeError.None ? string.Empty : look?.Garments.FirstOrDefault(g => g.IsTell)?.Label ?? string.Empty;

    /// <summary>True when the traveller has a deviation fault (traveller types P1): a lie about their home, or a costume error. Denying one needs a logged deviation.</summary>
    public bool HasDeviationFault => IsLiar || costumeFault != CostumeError.None;

    /// <summary>True if the claimed destination is permitted by today's rules.</summary>
    public bool claimAllowedByRules = true;

    /// <summary>The traveller's claim sentence (their kind's interview.claims line with the claimed place's label); the banner, the shift summary and the transcript's second line.</summary>
    public string claimLine;

    /// <summary>The traveller's answer to each question askable today, in question order (computed at generation from the same values as the papers).</summary>
    public readonly List<InterviewAnswer> answers = new();

    /// <summary>What the traveller says when asked small talk (their claimed place's or era's flavour; null when none is authored).</summary>
    public LineText smallTalk;

    /// <summary>
    /// How the traveller looks (layers and garments; a premade: one whole
    /// picture), composed at generation from the claim; a liar's dress tell is
    /// one garment from the true home. Null only when the case has no blueprint.
    /// </summary>
    public TravellerLook look;

    /// <summary>
    /// The correct decision: accept only a traveller with no deviation fault
    /// whose destination is permitted today; deny a liar, a costume error or
    /// a rule-breaking destination (VerdictRules' liar input is the deviation
    /// fault until the plan's phase 7 names it).
    /// </summary>
    public bool ShouldAccept => VerdictRules.ShouldAccept(HasDeviationFault, claimAllowedByRules);

    /// <summary>Runtime documents built from templates.</summary>
    public readonly List<DocumentInstance> documents = new();
}

/// <summary>
/// A runtime document built from a template: its structured fields, in the template's order.
/// </summary>
public sealed class DocumentInstance
{
    /// <summary>Template this document was built from.</summary>
    public DocumentTemplateSO template;

    /// <summary>Structured, checkable fields (investigation feature).</summary>
    public readonly List<DocumentField> fields = new();

    /// <summary>The document's name as every view shows it: its template's display name, else the untitled line (audit R4-006: one source).</summary>
    public string DisplayName => template != null && !string.IsNullOrEmpty(template.displayName) ? template.displayName : UiText.Get("document.untitled");

    /// <summary>The paper's serial, printed with its barcode ("TC-610/583021"; FormSerials, set by CaseFactory).</summary>
    public string serial = string.Empty;
}
