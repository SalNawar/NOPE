/// <summary>
/// What the investigation desk can do with the parts the office builder wired
/// (audit R4-022; the PC redesign RF1). A case shows only when the desk
/// windows are wired (the document window template, the window layer, Accept
/// and Deny; there is no text fallback any more). A traveller's answers can be
/// read only with the wheel's ring, the transcript and its chrome; garments
/// can be compared only with the ring (its "Look >" menu) and the compare;
/// the evidence system needs the desk windows and the compare; papers go onto
/// the desk only when the desk windows and the desk with all its parts are
/// wired. GameManager generates no spoken or dress tell for a day whose
/// interview or look is unreachable, and gates denials on evidence only while
/// the evidence system is active, so these rows decide the day's content. The
/// Missing flags are the start-up warnings. Pure; InvestigationUIController
/// evaluates it from its references.
/// </summary>
public readonly struct InvestigationWiring
{
    /// <summary>
    /// Evaluates the wired parts: the document window template, the window
    /// layer, Accept, Deny, the compare, the wheel's ring, the transcript
    /// window, its chrome, the desk with all its parts, and the Citizen Records app.
    /// </summary>
    public InvestigationWiring(bool documentTemplate, bool windowLayer, bool accept, bool deny, bool compare,
                               bool ring, bool transcript, bool transcriptChrome, bool desk, bool records)
    {
        Wired = documentTemplate && windowLayer && accept && deny;
        EvidenceSystemActive = Wired && compare;
        InterviewReachable = ring && transcript && transcriptChrome;
        AppearanceReachable = ring && compare;
        DeskReachable = Wired && desk;
        RecordsMissing = EvidenceSystemActive && !records;
    }

    /// <summary>The desk windows a case needs are wired (otherwise no case can be shown).</summary>
    public bool Wired { get; }

    /// <summary>The evidence loop is playable (the desk windows and the compare), so scoring may gate denials on documented evidence.</summary>
    public bool EvidenceSystemActive { get; }

    /// <summary>A traveller's answers can be read: the ring, the transcript and its chrome.</summary>
    public bool InterviewReachable { get; }

    /// <summary>A traveller's garments can be looked at and compared: the ring and the compare.</summary>
    public bool AppearanceReachable { get; }

    /// <summary>Documents become physical papers: the desk windows and the desk with all its parts (a partly wired desk takes the window path).</summary>
    public bool DeskReachable { get; }

    /// <summary>Warn: the evidence system is active but Citizen Records is not wired, so birth-date tells cannot be proven.</summary>
    public bool RecordsMissing { get; }

    /// <summary>Warn: the desk windows cannot read answers, so questions are hidden and no tell is spoken today.</summary>
    public bool InterviewMissing => Wired && !InterviewReachable;

    /// <summary>Warn: the desk windows cannot compare garments, so no dress tell is generated today.</summary>
    public bool AppearanceMissing => Wired && !AppearanceReachable;

    /// <summary>Warn: there is no desk, so documents open on the PC when handed over.</summary>
    public bool DeskMissing => Wired && !DeskReachable;
}
