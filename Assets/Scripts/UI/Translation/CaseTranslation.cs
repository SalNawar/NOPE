using TMPro;

/// <summary>
/// One traveller's speech translation (piece 9; speech only since the
/// redesign's phase 1: papers are always English), paired up for the
/// transcript and the wheel: whether the claimed place's tongue is foreign
/// today, its look and script font, whether the region's Speech translator is
/// owned, the flip's knobs and the motion choice, and the compare bar's
/// placeholder. Each member is one call into tested rules:
/// Translation.InTongue (Domain) decides who speaks the tongue, DisplayText
/// (Visuals) how it shows.
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
    public CaseTranslation(ForeignText text, TMP_FontAsset font, bool speechTranslated,
                           FlipTiming timing, bool reducedMotion, string placeholder)
    {
        Foreign = true;
        Text = text;
        Font = font;
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

    /// <summary>True when the region's Speech translator was owned at the start of the day.</summary>
    public bool SpeechTranslated { get; }

    /// <summary>The flip's knobs.</summary>
    public FlipTiming Timing { get; }

    /// <summary>True when the player chose reduced motion (translations show at once).</summary>
    public bool ReducedMotion { get; }

    /// <summary>The compare bar's text for an untranslated side: "(untranslated Egyptian; Near East Translator)".</summary>
    public string Placeholder { get; }

    /// <summary>A transcript line, settled: plain when not foreign, the desk's, or with the Speech translator; else untranslated.</summary>
    public Reveal Line(DialogSpeaker speaker) =>
        !Foreign || !Translation.InTongue(speaker) || SpeechTranslated ? Reveal.Plain : Reveal.Untranslated(Text);

    /// <summary>The bubble's current traveller line <paramref name="lineSeconds"/> after it started: plain, untranslated, or flipping.</summary>
    public Reveal Bubble(float lineSeconds)
    {
        if (!Foreign)
            return Reveal.Plain;
        return SpeechTranslated ? Reveal.Flipping(Text, lineSeconds) : Reveal.Untranslated(Text);
    }

    /// <summary>The compare bar's text for a statement: the canonical value when it reads (not foreign, not in the tongue, or translated), else the placeholder. The evidence stays canonical either way.</summary>
    public string Shown(bool inTongue, bool translated, string canonical) =>
        !Foreign || !inTongue || translated ? canonical : Placeholder;
}
