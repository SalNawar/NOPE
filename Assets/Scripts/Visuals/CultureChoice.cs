/// <summary>Which language a themed desk's labels are in, and why (piece 6 U8, U12).</summary>
public enum LabelLanguage
{
    /// <summary>The culture's own labels.</summary>
    Culture,

    /// <summary>The culture speaks the reading language (Britain, neutral): no table needed.</summary>
    SameAsReading,

    /// <summary>The content has no table for the culture's language.</summary>
    NoTable,

    /// <summary>"Always English" in Settings (the default before the Translation Lens's day: CultureChoice.EnglishBySetting).</summary>
    EnglishBySetting,

    /// <summary>No installed font can draw the culture's labels.</summary>
    EnglishNoFont
}

/// <summary>
/// The presentation choices the culture theme service makes, as pure rules
/// (piece 6 R22): the label language, the wallet's word and a text's style.
/// </summary>
public static class CultureChoice
{
    /// <summary>
    /// The label language: SameAsReading when the two languages are equal
    /// (ordinal), else NoTable, else EnglishBySetting, else EnglishNoFont, else
    /// Culture, checked in that order. Only Culture gives the desk the culture's labels.
    /// </summary>
    public static LabelLanguage Language(string themeLanguage, string readingLanguage, bool hasCultureTable, bool alwaysEnglish, bool fontCovers)
    {
        if (string.Equals(themeLanguage, readingLanguage, System.StringComparison.Ordinal))
            return LabelLanguage.SameAsReading;
        if (!hasCultureTable)
            return LabelLanguage.NoTable;
        if (alwaysEnglish)
            return LabelLanguage.EnglishBySetting;
        if (!fontCovers)
            return LabelLanguage.EnglishNoFont;
        return LabelLanguage.Culture;
    }

    /// <summary>The value Settings stores for "Follow history" (UiLanguagePreference); any other value, or none, reads as "Always English".</summary>
    public const string FollowHistoryChoice = "history";

    /// <summary>The value Settings stores for "Always English" (UiLanguagePreference).</summary>
    public const string AlwaysEnglishChoice = "english";

    /// <summary>
    /// True when the stored Settings choice is "Always English": any value but
    /// <see cref="FollowHistoryChoice"/>, none included (the default: track
    /// LANG's decision of 2026-10-08, after Saleh's playtest of demo 1008a,
    /// "second day language switched already and also hovering didn't
    /// translate": a player reads English until the Translation Lens's day
    /// unless they opt into Follow history).
    /// </summary>
    public static bool ChoseEnglish(string storedChoice) =>
        !string.Equals(storedChoice, FollowHistoryChoice, System.StringComparison.Ordinal);

    /// <summary>
    /// True when the labels read English by the player's setting: the stored
    /// choice is English (<see cref="ChoseEnglish"/>, the default) and the
    /// language is not locked yet. From the Translation Lens's day
    /// (<paramref name="languageLocked"/>: TranslationLens.LanguageLocked) the
    /// labels follow history whatever the choice, and the lens reads them.
    /// </summary>
    public static bool EnglishBySetting(string storedChoice, bool languageLocked) =>
        !languageLocked && ChoseEnglish(storedChoice);

    /// <summary>The wallet's word: the Future currency when a culture leads and its Future place has one, else the fallback (the neutral word).</summary>
    public static string Wallet(string cultureId, string futureCurrency, string fallback) =>
        !string.IsNullOrWhiteSpace(cultureId) && !string.IsNullOrWhiteSpace(futureCurrency) ? futureCurrency : fallback;

    /// <summary>
    /// True when a label holds a character the Latin fallback font cannot draw:
    /// anything above U+03FF except general punctuation (U+2000–U+206F). The
    /// runtime LiberationSans draws Latin and Greek, so such a culture needs an
    /// OS font candidate.
    /// </summary>
    public static bool NeedsOsFont(string text)
    {
        foreach (char c in text ?? string.Empty)
            if (c > '\u03FF' && (c < '\u2000' || c > '\u206F'))
                return true;
        return false;
    }

    /// <summary>A text's style: the builder's style, italics dropped when the theme strips them (CJK, Arabic), plus the theme's addition (TMP FontStyles as int flags).</summary>
    public static int ComposeStyle(int baseStyle, int italicFlag, bool stripItalic, int add) =>
        (stripItalic ? baseStyle & ~italicFlag : baseStyle) | add;
}
