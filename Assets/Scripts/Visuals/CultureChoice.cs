/// <summary>Which language a text is printed in, and why (piece 6 U8, U12; the surfaces of D7, TEXT-LANG).</summary>
public enum LabelLanguage
{
    /// <summary>The culture's own labels (a paper or a sign, when the culture's language applies).</summary>
    Culture,

    /// <summary>The culture speaks the reading language (Britain, neutral): no table needed.</summary>
    SameAsReading,

    /// <summary>The content has no table for the culture's language.</summary>
    NoTable,

    /// <summary>English on the papers by the player's setting: "Always English", or no choice before the culture day (CultureChoice.EnglishBySetting).</summary>
    EnglishBySetting,

    /// <summary>No installed font can draw the culture's labels.</summary>
    EnglishNoFont,

    /// <summary>The PC: always the reading language, whatever leads (Saleh 2026-10-18: "language in the pc shouldn't change"; D7: "its a pc you set the language").</summary>
    EnglishOnPc,

    /// <summary>The chrome around the papers (report, Home, citation, verdict, wheel, guide, memo, briefing, the desk's controls): always the player's language (BUG-3a, D7).</summary>
    PlayerLanguage
}

/// <summary>
/// Where a text is printed (D7, Saleh 2026-10-20: "papers will change
/// language and the pc background ... its a pc you set the language"): only a
/// paper's printed words and the newspaper take the world's language (and
/// in-world signs, our proposal); everything else reads in the player's
/// language. Serialized nowhere; append-only all the same.
/// </summary>
public enum TextSurface
{
    /// <summary>The chrome at the desk and around it: the desk's controls and labels, the report, Home, the citation, the verdict, the wheel, the guide, the memo and the briefing (the player's language).</summary>
    Desk,

    /// <summary>The PC: every app, window, the taskbar, the Start menu, the icons, Settings and the clock (the player's language; its look changes, PC-LANG-FX).</summary>
    Pc,

    /// <summary>A paper's printed words and the newspaper (the world's language, by the culture rule).</summary>
    Paper,

    /// <summary>An in-world sign in the hall (follows Paper: our proposal, D7 left signs open).</summary>
    Sign
}

/// <summary>
/// The presentation choices the culture theme service makes, as pure rules
/// (piece 6 R22): the label language, which surface takes it, the wallet's
/// word and a text's style.
/// </summary>
public static class CultureChoice
{
    /// <summary>
    /// The papers' label language: SameAsReading when the two languages are
    /// equal (ordinal), else NoTable, else EnglishBySetting, else
    /// EnglishNoFont, else Culture, checked in that order. Only Culture gives
    /// a paper (or a sign) the culture's labels (<see cref="Surface"/>).
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

    /// <summary>True for the surfaces that take the world's language (a paper, the newspaper, a sign); every other surface reads the player's language (BUG-3a).</summary>
    public static bool FollowsWorld(TextSurface surface) => surface == TextSurface.Paper || surface == TextSurface.Sign;

    /// <summary>
    /// The language a surface prints in: the PC always the reading language
    /// (EnglishOnPc), so a scanned paper's copy on the PC reads in English
    /// and scanning is how a foreign paper is read cleanly (Saleh
    /// 2026-10-18); the chrome always the player's (PlayerLanguage: the
    /// report, Home, the citation, the verdict, the wheel, the guide, the memo
    /// and the briefing never turn into the world's language, BUG-3a); a paper
    /// or a sign <paramref name="paper"/>, the culture rule's (<see cref="Language"/>).
    /// </summary>
    public static LabelLanguage Surface(TextSurface surface, LabelLanguage paper) =>
        surface == TextSurface.Pc ? LabelLanguage.EnglishOnPc : FollowsWorld(surface) ? paper : LabelLanguage.PlayerLanguage;

    /// <summary>True when a language prints the culture's own labels.</summary>
    public static bool ShowsCulture(LabelLanguage language) => language == LabelLanguage.Culture;

    /// <summary>The value Settings stores for "Follow history" (UiLanguagePreference): the papers take the world's language from day 1.</summary>
    public const string FollowHistoryChoice = "history";

    /// <summary>The value Settings stores for "Always English" (UiLanguagePreference): the papers stay English.</summary>
    public const string AlwaysEnglishChoice = "english";

    /// <summary>
    /// True when the papers read English by the player's setting. The setting
    /// never locks (D7, "you set the language"; BUG-3a): "Always English"
    /// keeps them English on any day, "Follow history" gives them the world's
    /// language on any day, and a player who never chose reads English until
    /// the culture day (<paramref name="cultureDay"/>: the Translation Lens's
    /// day, TranslationLens.LanguageLocked; track LANG's default of
    /// 2026-10-08, after Saleh's playtest of demo 1008a) and the world's
    /// language from then on. Any other stored value reads as no choice.
    /// </summary>
    public static bool EnglishBySetting(string storedChoice, bool cultureDay)
    {
        if (string.Equals(storedChoice, AlwaysEnglishChoice, System.StringComparison.Ordinal))
            return true;
        if (string.Equals(storedChoice, FollowHistoryChoice, System.StringComparison.Ordinal))
            return false;
        return !cultureDay;
    }

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
            if (c > 'Ͽ' && (c < ' ' || c > '⁯'))
                return true;
        return false;
    }

    /// <summary>A text's style: the builder's style, italics dropped when the theme strips them (CJK, Arabic), plus the theme's addition (TMP FontStyles as int flags).</summary>
    public static int ComposeStyle(int baseStyle, int italicFlag, bool stripItalic, int add) =>
        (stripItalic ? baseStyle & ~italicFlag : baseStyle) | add;
}
