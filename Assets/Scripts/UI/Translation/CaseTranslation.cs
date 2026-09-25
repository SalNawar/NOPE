using TMPro;

/// <summary>
/// One traveller's translation (piece 9), paired up for the windows and the
/// wheel: whether the claimed place's tongue is foreign today, its look and
/// script font, which translators are owned, the flip's knobs and the motion
/// choice, and the compare bar's placeholder. Each member is one call into
/// tested rules: Translation.InTongue (Domain) decides what uses the tongue,
/// DisplayText (Visuals) how it shows.
/// </summary>
public sealed class CaseTranslation
{
    /// <summary>Everything plain: a native or unknown tongue, before fromDay, or no translation data.</summary>
    public static CaseTranslation None { get; } = new CaseTranslation();

    private CaseTranslation()
    {
        Timing = new FlipTiming();
        Placeholder = string.Empty;
    }

    /// <summary>A traveller whose tongue is foreign today.</summary>
    public CaseTranslation(ForeignText text, TMP_FontAsset font, bool papersTranslated, bool speechTranslated,
                           FlipTiming timing, bool reducedMotion, string placeholder)
    {
        Foreign = true;
        Text = text;
        Font = font;
        PapersTranslated = papersTranslated;
        SpeechTranslated = speechTranslated;
        Timing = timing ?? new FlipTiming();
        ReducedMotion = reducedMotion;
        Placeholder = placeholder ?? string.Empty;
    }

    /// <summary>True when the claimed place's tongue is foreign today.</summary>
    public bool Foreign { get; }

    /// <summary>The tongue's look (its table and direction, or the fallback cipher); null when not foreign.</summary>
    public ForeignText Text { get; }

    /// <summary>The script's runtime font for foreign cells; null = the text's own font (the fallback cipher).</summary>
    public TMP_FontAsset Font { get; }

    /// <summary>True when the region's Papers translator was owned at the start of the day.</summary>
    public bool PapersTranslated { get; }

    /// <summary>True when the region's Speech translator was owned at the start of the day.</summary>
    public bool SpeechTranslated { get; }

    /// <summary>The flip's knobs.</summary>
    public FlipTiming Timing { get; }

    /// <summary>True when the player chose reduced motion (translations show at once).</summary>
    public bool ReducedMotion { get; }

    /// <summary>The compare bar's text for an untranslated side: "(untranslated Egyptian; Near East Translator)".</summary>
    public string Placeholder { get; }

    /// <summary>
    /// A document field <paramref name="revealElapsed"/> seconds after its
    /// scan (NaN before) as document row <paramref name="row"/>: plain when
    /// not foreign or not in the tongue (the name and the date of birth);
    /// untranslated without the Papers translator or before the scan; else flipping.
    /// </summary>
    public Reveal Field(ClueCategory category, float revealElapsed, int row)
    {
        if (!Foreign || !Translation.InTongue(category))
            return Reveal.Plain;
        return PapersTranslated && !float.IsNaN(revealElapsed) ? Reveal.Flipping(Text, revealElapsed, row) : Reveal.Untranslated(Text);
    }

    /// <summary>A transcript line, settled: plain when not foreign, the desk's, or with the Speech translator; else untranslated.</summary>
    public Reveal Line(DialogSpeaker speaker) =>
        !Foreign || !Translation.InTongue(speaker) || SpeechTranslated ? Reveal.Plain : Reveal.Untranslated(Text);

    /// <summary>The bubble's current traveller line <paramref name="lineSeconds"/> after it started: plain, untranslated, or flipping.</summary>
    public Reveal Bubble(float lineSeconds)
    {
        if (!Foreign)
            return Reveal.Plain;
        return SpeechTranslated ? Reveal.Flipping(Text, lineSeconds, 0) : Reveal.Untranslated(Text);
    }

    /// <summary>The compare bar's text for a statement: the canonical value when it reads (not foreign, not in the tongue, or translated), else the placeholder. The evidence stays canonical either way.</summary>
    public string Shown(bool inTongue, bool translated, string canonical) =>
        !Foreign || !inTongue || translated ? canonical : Placeholder;
}
