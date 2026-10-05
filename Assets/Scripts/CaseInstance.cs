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

    /// <summary>True for a famous premade (Premades.IsFamous: the displaced kind): scored as a legendary; a 2150 citizen premade is scored as anyone.</summary>
    public bool IsFamous => isLegendary && legendarySource != null && Premades.IsFamous(legendarySource.kind);

    /// <summary>The forced entry that stands in this slot today (DayPlanSO forced cases, days 7-15 B9: its premade or blueprint, its fault and its voice); null for a traveller the day drew.</summary>
    public ForcedCaseSlot forcedAppearance;

    /// <summary>The narrative dialog bound to this traveller (Premades.Voice: the appearance's, else the premade's; blank for an ordinary traveller): offered only while they are at the desk (InterviewDay.OfferedDialogs).</summary>
    public string premadeDialogId = string.Empty;

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

    /// <summary>The slot's case seed (Seeds.ForCase): a denied traveller's return is drawn on Seeds.ForReturn of it, and their return reseeds their look and account streams with it (wave 5, lesson 9).</summary>
    public int caseSeed;

    /// <summary>The chance this liar cracks when the desk asks about a logged difference: their personality's confess (Confrontations.Outcome; wave 5, lesson 3); 0 without one.</summary>
    public float confess;

    /// <summary>A returning traveller's record (WorldState.returns: who they were, when they were denied, what they come back with); null on a first visit (wave 5, lesson 9).</summary>
    public ReturningTraveller returning;

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
    /// The nation whose passport the traveller carries (the travel documents
    /// spec, TD3: its cover, emblem and code): a 2150 citizen's family
    /// country (the list their name came from; a story character's family),
    /// anyone else's stated home (the claimed nation). Null when neither is
    /// known; the passport then wears its form's own cover.
    /// </summary>
    public NationSO passportNation;

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

    /// <summary>What the Directives read of the traveller (CaseFactory's facts: the kind, the closure, the classes, the forms, the waiver, the standing, the dates): the workbench holds today's rules against their values with them (RuleChecks, the PC workbench spec §4.3). A waiver signed at the desk swaps in <see cref="factsWithDeskWaiver"/>.</summary>
    [System.NonSerialized] public CaseFacts facts;

    /// <summary>The facts with the pad's waiver carried and signed (null when their kind carries none).</summary>
    [System.NonSerialized] public CaseFacts factsWithDeskWaiver;

    /// <summary>Why the traveller lacks a form the desk asks for: Honest (they never needed it), or Missing when a broken paper set left their waiver or proof of means out (CaseFactory.BreakPapers; the interview's reply, MissingFormVariant).</summary>
    public MissingFormVariant missingFormVariant;

    /// <summary>True when the Directives forbid the traveller's claim or papers (a directive fault).</summary>
    public bool HasDirectiveFault => directiveFault != DirectiveFault.None;

    /// <summary>What a citation slip names about the traveller (lesson 6; CaseFactory at generation): their fault's rule, its memo row and the exact values, or for an honest traveller the line a wrong denial prints. Null only for a case with no blueprint.</summary>
    public CitationFacts citation;

    /// <summary>The traveller's one fault reason (Faults.Reason): a wrong accept's citation key suffix; empty with no fault.</summary>
    public string FaultReason => Faults.Reason(directiveFault, costumeFault, lie);

    /// <summary>The traveller's answer to each question askable today, in question order (computed at generation from the same values as the papers).</summary>
    public readonly List<InterviewAnswer> answers = new();

    /// <summary>What the traveller says when asked small talk (Voices.SmallTalk: their personality's, their home's or their kind's line; null when none is authored).</summary>
    public LineText smallTalk;

    /// <summary>The liar's slip, said once after their small-talk reply (the personalities spec's T9-T11: a generated liar who rolled under the day's slipChance, or a liar premade with a slip line; Voices.Slip); null for none. Never evidence.</summary>
    public LineText slip;

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

    /// <summary>Someone else's look on the traveller's photo (the SwappedPhoto lie, Looks.Stranger; the document design spec, D8); null when the photo is their own.</summary>
    public TravellerLook strangerPhoto;

    /// <summary>Who the papers' photo shows: a stranger's look for someone else's photo, else the traveller's own.</summary>
    public TravellerLook PhotoLook => strangerPhoto ?? look;

    /// <summary>
    /// The correct decision (traveller types §5.2): accept only a traveller
    /// with no fault; deny a deviation fault (a liar, a smuggler, a forger, a
    /// costume error) or a directive fault (a closed destination, a wrong
    /// departure date, an expired paper).
    /// </summary>
    public bool ShouldAccept => VerdictRules.ShouldAccept(HasDeviationFault, HasDirectiveFault);

    /// <summary>Runtime documents built from templates.</summary>
    public readonly List<DocumentInstance> documents = new();

    /// <summary>
    /// The Stranding Waiver the desk's pad would file for them (the endings
    /// and strandings spec §7.3): the waiver form filled from their Citizen
    /// Account (its registered number and unit) and signed in their hand;
    /// null when their kind carries no waiver (the pad is not theirs to sign).
    /// Filed only when they sign (<see cref="SignWaiverAtDesk"/>).
    /// </summary>
    public DocumentInstance deskWaiver;

    /// <summary>Their directive fault once a waiver is carried and signed (Directives.Fault over the finished papers with the pad's waiver): what signing at the desk leaves.</summary>
    public DirectiveFault faultWithDeskWaiver;

    /// <summary>Their answer to the desk's waiver pad (Waivers.PadReply, on their own stream, Seeds.ForWaiverSign).</summary>
    public WaiverPadReply waiverPadReply = WaiverPadReply.NotNeeded;

    /// <summary>True once they signed the pad's waiver and the desk filed it.</summary>
    public bool waiverSignedAtDesk;

    /// <summary>The directive fault the desk's signature cured (None when it cured nothing): denying them stays right (VerdictRules.IsCorrect).</summary>
    public DirectiveFault curedAtDesk;

    /// <summary>
    /// They sign the pad's waiver and the desk files it: a valid signed waiver
    /// on file, their directive fault becomes <see cref="faultWithDeskWaiver"/>
    /// and the fault it cured is remembered. False (nothing changes) when the
    /// pad is not theirs, they do not sign, or they signed already.
    /// </summary>
    public bool SignWaiverAtDesk()
    {
        if (deskWaiver == null || waiverSignedAtDesk || waiverPadReply != WaiverPadReply.Signs)
            return false;
        waiverSignedAtDesk = true;
        curedAtDesk = directiveFault != faultWithDeskWaiver ? directiveFault : DirectiveFault.None;
        directiveFault = faultWithDeskWaiver;
        if (factsWithDeskWaiver != null)
            facts = factsWithDeskWaiver;
        return true;
    }

    /// <summary>Every waiver of theirs, as fields: the ones they carry and the desk's filed copy (Waivers.OnFile reads them).</summary>
    public IEnumerable<IReadOnlyList<DocumentField>> WaiverPapers
    {
        get
        {
            foreach (DocumentInstance d in documents)
                if (d != null && d.template != null && d.template.formNumber == Directives.Waiver)
                    yield return d.fields;
            if (waiverSignedAtDesk && deskWaiver != null)
                yield return deskWaiver.fields;
        }
    }

    /// <summary>True when the agency issued the Stranding Waiver on the traveller's day (DayPlanSO.Issues, lesson D7; set at generation): a stranding without one on file is fined only then (Strandings.Fine).</summary>
    public bool waiverIssued;

    /// <summary>True when a valid signed waiver of theirs is on file (Waivers.OnFile against their account's registered number and unit).</summary>
    public bool Waivered => account != null && Waivers.OnFile(WaiverPapers, account.WaiverNo, account.Transponder);
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
