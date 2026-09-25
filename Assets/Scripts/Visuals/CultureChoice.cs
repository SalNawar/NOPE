/// <summary>Which language a themed desk's labels are in, and why (piece 6 U8, U12).</summary>
public enum LabelLanguage
{
    /// <summary>The culture's own labels.</summary>
    Culture,

    /// <summary>The culture speaks the reading language (Britain, neutral): no table needed.</summary>
    SameAsReading,

    /// <summary>The content has no table for the culture's language.</summary>
    NoTable,

    /// <summary>The player chose "Always English" in Settings.</summary>
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

    /// <summary>The wallet's word: the Future currency when a culture leads and its Future place has one, else the fallback (the neutral word).</summary>
    public static string Wallet(string cultureId, string futureCurrency, string fallback) =>
        !string.IsNullOrWhiteSpace(cultureId) && !string.IsNullOrWhiteSpace(futureCurrency) ? futureCurrency : fallback;

    /// <summary>A text's style: the builder's style, italics dropped when the theme strips them (CJK, Arabic), plus the theme's addition (TMP FontStyles as int flags).</summary>
    public static int ComposeStyle(int baseStyle, int italicFlag, bool stripItalic, int add) =>
        (stripItalic ? baseStyle & ~italicFlag : baseStyle) | add;
}
