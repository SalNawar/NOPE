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

    /// <summary>
    /// A 2150 citizen's Citizen Account (AccountMaker.Make, on their account
    /// stream; traveller types R1): the truth their papers print and the
    /// Records app shows. Null for the displaced, or when the agency calendar
    /// cannot count today.
    /// </summary>
    public CitizenAccount account;

    /// <summary>True if this traveller is a premade character (named, drawn whole).</summary>
    public bool isLegendary;

    /// <summary>The premade's asset (null for a generated traveller).</summary>
    public LegendarySO legendarySource;

    /// <summary>Visitor archetype (drives default timeline impacts + tags).</summary>
    public ArchetypeSO archetype;

    /// <summary>Label of the claimed place, "Abbasid Baghdad (Medieval)" (claim line and Citizen Records).</summary>
    public string originLabel;

    /// <summary>The tongue the traveller speaks (piece 9): the claimed place's for the displaced, never their true home's; empty (English) for a 2150 citizen (traveller types I3) or without a place. Papers are always English.</summary>
    public string tongueId = string.Empty;

    /// <summary>Authored impact overrides from the blueprint/legendary (may be empty).</summary>
    public readonly List<TimelineImpact> authoredImpacts = new();

    /// <summary>Display name for the visitor (given name + role suffix).</summary>
    public string visitorDisplayName;

    /// <summary>The registered given name (a liar's cover name; citizen-records lookup key): from the claimed place's names, or for a 2150 citizen the Future places' lists together (traveller types K4).</summary>
    public string visitorGivenName;

    /// <summary>
    /// The registered date of birth (what the agency has on file): from the
    /// claimed place's birth years, or for a 2150 citizen the present's
    /// (2080-2132). A birth-date tell prints a different year on the papers.
    /// </summary>
    public string trueBirthDate;

    /// <summary>Gender from the name list the given name came from (the claimed place's, or a citizen's merged 2150 lists), or the premade's (Unknown for "Subject #n").</summary>
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
    /// path). A liar's real era is tellSourceEraId. Set even when the case
    /// has no blueprint (audit R3-020 merged the duplicate `trueEra`).
    /// </summary>
    public EraSO claimedEra;

    /// <summary>
    /// Where a liar's tells really come from, the tell source (traveller
    /// types H3): the nation id of another of today's places (a false
    /// origin, L7) or of the present (a fake displaced person, L8; a
    /// smuggler, L6); null for an honest traveller (whose home is the
    /// claim). Ids rather than a profile: the present is no
    /// NationEraProfileSO, and a carry reads the ids against today's facts
    /// (HistoryService.RecordCarry).
    /// </summary>
    public string tellSourceNationId;

    /// <summary>The era id of the tell source (the present's era for a fake displaced person or a smuggler); null for an honest traveller.</summary>
    public string tellSourceEraId;

    /// <summary>The tell source's label, from today's FactTable like <see cref="originLabel"/> (empty for an honest traveller).</summary>
    public string trueHomeLabel = string.Empty;

    /// <summary>The lie the traveller carries, once planned and printed (Lies.Roll's pick that could show); null for an honest traveller.</summary>
    public LieKind? lie;

    /// <summary>True when the traveller carries a place lie (a false origin, a fake displaced person, smuggling): their papers, answers or dress leak the tell source's values.</summary>
    public bool IsLiar => !string.IsNullOrEmpty(tellSourceEraId);

    /// <summary>Where a place liar's tells come from, as a label; the claim's for everyone else (verdict and logs).</summary>
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

    /// <summary>
    /// The record tells a forger prints (RecordLies, traveller types L2):
    /// the forged categories on their papers, each disproved by the
    /// traveller's own Citizen Account and, where two papers disagree, by
    /// each other (the cross proof). Empty for everyone else.
    /// </summary>
    public IReadOnlyList<RecordTell> recordTells = System.Array.Empty<RecordTell>();

    /// <summary>True when the traveller's papers forge record fields (a record lie: poor posing as rich, a doctored identity).</summary>
    public bool IsForger => recordTells.Count > 0;

    /// <summary>True when the traveller has a deviation fault (traveller types P1): a lie about their home, a record lie or a costume error. Denying one needs a logged deviation.</summary>
    public bool HasDeviationFault => IsLiar || IsForger || costumeFault != CostumeError.None;

    /// <summary>
    /// The traveller's directive fault (traveller types P1): what today's
    /// Directives forbid in the claim or the papers, read against them and
    /// the agency calendar with no evidence needed (a closed destination; a
    /// departure dated another day or an expired paper, Directives.PaperDates);
    /// None when the Directives allow them.
    /// </summary>
    public DirectiveFault directiveFault;

    /// <summary>Why the traveller lacks a form the desk asks for: Honest (they never needed it), or Missing when a broken paper set left their waiver or proof of means out (CaseFactory.BreakPapers; the interview's reply, MissingFormVariant).</summary>
    public MissingFormVariant missingFormVariant;

    /// <summary>True when the Directives forbid the traveller's claim or papers (a directive fault).</summary>
    public bool HasDirectiveFault => directiveFault != DirectiveFault.None;

    /// <summary>The traveller's one fault reason (Faults.Reason): a wrong accept's citation key suffix; empty with no fault.</summary>
    public string FaultReason => Faults.Reason(directiveFault, costumeFault, lie);

    /// <summary>The traveller's answer to each question askable today, in question order (computed at generation from the same values as the papers).</summary>
    public readonly List<InterviewAnswer> answers = new();

    /// <summary>What the traveller says when asked small talk (Voices.SmallTalk: their personality's, their home's or their kind's line; null when none is authored).</summary>
    public LineText smallTalk;

    /// <summary>The traveller's personality (Personality.id), drawn at generation on its own stream (Seeds.ForPersonality); blank for a premade, who speaks its own lines, and for an empty cast. Never printed (the personalities spec's PS4).</summary>
    public string personality = string.Empty;

    /// <summary>The traveller's dialog seed (Seeds.ForDialog): every line pick is a value of it and the slot's key (Voices.Pick), never a draw.</summary>
    public int dialogSeed;

    /// <summary>Who speaks: the traveller's personality, or the premade they are, and their dialog seed (every reply is resolved in it, Voices).</summary>
    public Voice Voice => new Voice(personality, legendarySource != null ? legendarySource.id : null, dialogSeed);

    /// <summary>
    /// How the traveller looks (layers and garments; a premade: one whole
    /// picture), composed at generation from the claim; a liar's dress tell is
    /// one garment from the true home. Null only when the case has no blueprint.
    /// </summary>
    public TravellerLook look;

    /// <summary>
    /// The correct decision (traveller types §5.2): accept only a traveller
    /// with no fault; deny a deviation fault (a liar, a smuggler, a forger, a
    /// costume error) or a directive fault (a closed destination, a wrong
    /// departure date, an expired paper).
    /// </summary>
    public bool ShouldAccept => VerdictRules.ShouldAccept(HasDeviationFault, HasDirectiveFault);

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
