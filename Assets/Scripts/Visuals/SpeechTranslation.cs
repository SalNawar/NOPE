using System.Collections.Generic;

/// <summary>
/// One traveller's speech translation (piece 9; speech only since the
/// redesign's phase 1: every document is filled in English), fixed when the
/// traveller is presented: whether their claimed place's tongue is foreign
/// today, its look, whether the region's Speech translator is owned, the
/// flip's knobs, the motion choice and the compare bar's placeholder; and the
/// decisions the transcript, the bubble and the compare bar read. Pure, so the
/// decision table is tested headless (audit R4-021: it lived in the untestable
/// CaseTranslation, which now pairs it with the script's font).
/// </summary>
public sealed class SpeechTranslation
{
    private readonly ForeignText _text;
    private readonly bool _foreign;
    private readonly bool _translated;
    private readonly string _placeholder;

    /// <summary>Everything plain: a native or unknown tongue, before fromDay, or no translation data.</summary>
    public static SpeechTranslation None { get; } = new SpeechTranslation();

    private SpeechTranslation()
    {
        Timing = new FlipTiming();
        _placeholder = string.Empty;
    }

    /// <summary>
    /// A traveller whose tongue is foreign today: its look (<paramref name="text"/>),
    /// whether the Speech translator was owned at the start of the day, the flip's
    /// knobs (null: the defaults), the motion choice and the compare bar's
    /// placeholder for an untranslated answer (null: empty).
    /// </summary>
    public SpeechTranslation(ForeignText text, bool translated, FlipTiming timing, bool reducedMotion, string placeholder)
    {
        _foreign = true;
        _text = text;
        _translated = translated;
        Timing = timing ?? new FlipTiming();
        ReducedMotion = reducedMotion;
        _placeholder = placeholder ?? string.Empty;
    }

    /// <summary>The flip's knobs.</summary>
    public FlipTiming Timing { get; }

    /// <summary>True when the player chose reduced motion (translations show at once).</summary>
    public bool ReducedMotion { get; }

    /// <summary>A transcript line, settled: plain when not foreign, not in the tongue (<paramref name="inTongue"/> false: the desk's) or translated; else untranslated but for its <paramref name="english"/> spans (its key words).</summary>
    public Reveal Line(bool inTongue, IReadOnlyList<(int start, int length)> english) =>
        !_foreign || !inTongue || _translated ? Reveal.Plain : Reveal.Untranslated(_text, english);

    /// <summary>The bubble's traveller line <paramref name="lineSeconds"/> after it started: plain when not foreign, flipping on the line's clock when translated, else untranslated; its <paramref name="english"/> spans (its key words) always as they are.</summary>
    public Reveal Bubble(float lineSeconds, IReadOnlyList<(int start, int length)> english)
    {
        if (!_foreign)
            return Reveal.Plain;
        return _translated ? Reveal.Flipping(_text, lineSeconds, english) : Reveal.Untranslated(_text, english);
    }

    /// <summary>The compare bar's text for an answer: its canonical value when it reads (not foreign, not in the tongue, or translated), else the placeholder. The evidence stays canonical either way.</summary>
    public string Shown(bool inTongue, string canonical) =>
        !_foreign || !inTongue || _translated ? canonical : _placeholder;
}
