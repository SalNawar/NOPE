/// <summary>
/// What the investigation desk can do with the parts the office builder wired
/// (audit R4-022; the PC redesign RF1). The rich desk needs the document
/// window template, the window layer, Accept and Deny; without any of them
/// the text fallback runs, which prints answers and dress itself. In the rich
/// desk a traveller's answers can be read only with the wheel's ring, the
/// transcript and its chrome; garments can be compared only with the ring
/// (its "Look >" menu) and the compare; the evidence system needs the compare;
/// papers go onto the desk only when the desk and all its parts are wired.
/// GameManager generates no spoken or dress tell for a day whose interview or
/// look is unreachable, and gates denials on evidence only while the evidence
/// system is active, so these rows decide the day's content. The Missing
/// flags are the rich desk's start-up warnings. Pure; InvestigationUIController
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
        RichMode = documentTemplate && windowLayer && accept && deny;
        EvidenceSystemActive = RichMode && compare;
        InterviewReachable = !RichMode || (ring && transcript && transcriptChrome);
        AppearanceReachable = !RichMode || (ring && compare);
        DeskReachable = RichMode && desk;
        RecordsMissing = EvidenceSystemActive && !records;
    }

    /// <summary>The rich desk runs (windows, the compare, the desk); otherwise the text fallback.</summary>
    public bool RichMode { get; }

    /// <summary>The evidence loop is playable (the rich desk and the compare), so scoring may gate denials on documented evidence.</summary>
    public bool EvidenceSystemActive { get; }

    /// <summary>A traveller's answers can be read: always in the fallback; in the rich desk with the ring, the transcript and its chrome.</summary>
    public bool InterviewReachable { get; }

    /// <summary>A traveller's garments can be looked at and compared: always in the fallback; in the rich desk with the ring and the compare.</summary>
    public bool AppearanceReachable { get; }

    /// <summary>Documents become physical papers: the rich desk with the desk and all its parts (a partly wired desk takes the window path).</summary>
    public bool DeskReachable { get; }

    /// <summary>Warn: the evidence system is active but Citizen Records is not wired, so birth-date tells cannot be proven.</summary>
    public bool RecordsMissing { get; }

    /// <summary>Warn: the rich desk cannot read answers, so questions are hidden and no tell is spoken today.</summary>
    public bool InterviewMissing => RichMode && !InterviewReachable;

    /// <summary>Warn: the rich desk cannot compare garments, so no dress tell is generated today.</summary>
    public bool AppearanceMissing => RichMode && !AppearanceReachable;

    /// <summary>Warn: the rich desk has no desk, so documents open on the PC when handed over.</summary>
    public bool DeskMissing => RichMode && !DeskReachable;
}
