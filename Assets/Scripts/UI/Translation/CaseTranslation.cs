using TMPro;

/// <summary>
/// One traveller's speech translation for the transcript and the wheel
/// (piece 9; speech only since the redesign's phase 1: papers are always
/// English): the tested decisions (SpeechTranslation, Visuals) paired with the
/// script's runtime font, the one engine type they need. Each member is one
/// call into tested rules: Translation.InTongue (Domain) decides who speaks
/// the tongue, SpeechTranslation and DisplayText (Visuals) how it shows.
/// </summary>
public sealed class CaseTranslation
{
    /// <summary>Everything plain: a native or unknown tongue, before fromDay, or no translation data.</summary>
    public static CaseTranslation None { get; } = new CaseTranslation(SpeechTranslation.None, null, null, null);

    /// <summary>A traveller's speech translation, the script's runtime font (null: the text's own font, the fallback cipher) and the tongue (its id and name; null for None).</summary>
    public CaseTranslation(SpeechTranslation speech, TMP_FontAsset font, string tongueId, string tongueName)
    {
        Speech = speech ?? SpeechTranslation.None;
        Font = font;
        TongueId = tongueId;
        TongueName = tongueName;
    }

    /// <summary>The traveller's tongue's id (a copied untranslated line keeps it; null when everything is plain).</summary>
    public string TongueId { get; }

    /// <summary>The traveller's tongue as named ("Greek"; null when everything is plain).</summary>
    public string TongueName { get; }

    /// <summary>The decisions and knobs (foreign, translated, the flip's timing, the motion choice, the placeholder).</summary>
    public SpeechTranslation Speech { get; }

    /// <summary>The script's runtime font for foreign cells; null = the text's own font (the fallback cipher).</summary>
    public TMP_FontAsset Font { get; }

    /// <summary>A transcript line, settled: the traveller's in their tongue unless translated, its key words in English; the desk's always plain.</summary>
    public Reveal Line(DialogLine line) =>
        line != null ? Speech.Line(Translation.InTongue(line.Speaker), line.English) : Reveal.Plain;

    /// <summary>True when the transcript shows <paramref name="line"/> untranslated (its glyphs and English key words): a copy of it is a foreign clip.</summary>
    public bool Untranslated(DialogLine line) => Line(line).Kind == RevealKind.Untranslated;

    /// <summary>The bubble's traveller <paramref name="line"/> <paramref name="lineSeconds"/> after it started: plain, untranslated, or flipping, its key words in English (a null line: no key words).</summary>
    public Reveal Bubble(DialogLine line, float lineSeconds) => Speech.Bubble(lineSeconds, line != null ? line.English : null);

    /// <summary>The compare bar's text for an answer: its canonical value when it reads, else the placeholder.</summary>
    public string Shown(DialogLine answer) => Speech.Shown(Translation.InTongue(answer.Speaker), answer.Value);
}
