using NUnit.Framework;

/// <summary>The theme service's presentation rules: label language, wallet word, text style (piece 6 R22).</summary>
public class CultureChoiceTests
{
    [Test]
    public void Language_CultureWhenEverythingAllowsIt()
    {
        Assert.AreEqual(LabelLanguage.Culture, CultureChoice.Language("el", "en", true, false, true));
    }

    [Test]
    public void Language_SameAsReading_IsOrdinal()
    {
        Assert.AreEqual(LabelLanguage.SameAsReading, CultureChoice.Language("en", "en", false, true, false));
        Assert.AreNotEqual(LabelLanguage.SameAsReading, CultureChoice.Language("EN", "en", true, false, true));
    }

    [Test]
    public void Language_TheFallbacksInOrder()
    {
        Assert.AreEqual(LabelLanguage.NoTable, CultureChoice.Language("el", "en", false, true, false));
        Assert.AreEqual(LabelLanguage.EnglishBySetting, CultureChoice.Language("el", "en", true, true, true));
        Assert.AreEqual(LabelLanguage.EnglishBySetting, CultureChoice.Language("el", "en", true, true, false), "the setting wins over a missing font");
        Assert.AreEqual(LabelLanguage.EnglishNoFont, CultureChoice.Language("el", "en", true, false, false));
    }

    /// <summary>BUG-3a, D7 ("you set the language"): the setting never locks. Always English keeps the papers English on any day, Follow history gives them the world's language on any day, and no choice reads English until the culture day (the lens's day) and the world's language from then on.</summary>
    [TestCase(null, false, true, Description = "week 1, never chosen: English")]
    [TestCase(null, true, false, Description = "the culture day, never chosen: the world's language")]
    [TestCase("english", false, true)]
    [TestCase("english", true, true, Description = "never locked: Always English still holds on the culture day")]
    [TestCase("history", false, false, Description = "week 1, opted in: history")]
    [TestCase("history", true, false)]
    [TestCase("", true, false, Description = "an unknown value reads as no choice")]
    [TestCase("History", false, true, Description = "ordinal: only the stored value itself opts in")]
    public void EnglishBySetting_NeverLocks(string stored, bool cultureDay, bool expected)
    {
        Assert.AreEqual(expected, CultureChoice.EnglishBySetting(stored, cultureDay));
    }

    [Test]
    public void Language_DayTwoAfterACultureLeads_EnglishByDefault_CultureFromTheLensDay_UnlessAlwaysEnglish()
    {
        // Saleh's playtest of demo 1008a: day 1's decision made Greece lead and day 2 read Greek with no lens.
        Assert.AreEqual(LabelLanguage.EnglishBySetting, CultureChoice.Language("el", "en", true, CultureChoice.EnglishBySetting(null, false), true));
        Assert.AreEqual(LabelLanguage.Culture, CultureChoice.Language("el", "en", true, CultureChoice.EnglishBySetting(CultureChoice.FollowHistoryChoice, false), true));
        Assert.AreEqual(LabelLanguage.Culture, CultureChoice.Language("el", "en", true, CultureChoice.EnglishBySetting(null, true), true));
        Assert.AreEqual(LabelLanguage.EnglishBySetting, CultureChoice.Language("el", "en", true, CultureChoice.EnglishBySetting(CultureChoice.AlwaysEnglishChoice, true), true),
                        "the setting never locks (BUG-3a)");
    }

    [TestCase("c", "Digital yuan (e-CNY)", "Credits", "Digital yuan (e-CNY)")]
    [TestCase(null, "Digital yuan (e-CNY)", "Credits", "Credits")]
    [TestCase("  ", "Digital yuan (e-CNY)", "Credits", "Credits")]
    [TestCase("c", "", "Credits", "Credits")]
    [TestCase("c", null, "Credits", "Credits")]
    public void Wallet_TheFutureCurrencyOnlyWithACulture(string cultureId, string currency, string fallback, string expected)
    {
        Assert.AreEqual(expected, CultureChoice.Wallet(cultureId, currency, fallback));
    }

    [TestCase("ANNEHMEN", false)]
    [TestCase("ΕΓΚΡΙΣΗ", false, Description = "Greek: the Latin fallback draws it")]
    [TestCase("SHIFT LEDGER — EVENING EDITION", false, Description = "general punctuation")]
    [TestCase("قبول", true)]
    [TestCase("批准", true)]
    [TestCase("稳定度：{0:0}%", true)]
    [TestCase(null, false)]
    public void NeedsOsFont_BeyondLatinAndGreek(string text, bool expected)
    {
        Assert.AreEqual(expected, CultureChoice.NeedsOsFont(text));
    }

    [Test]
    public void ComposeStyle_StripsItalicsAndAdds()
    {
        const int bold = 1, italic = 2, smallCaps = 32;
        Assert.AreEqual(bold | smallCaps, CultureChoice.ComposeStyle(bold | italic, italic, true, smallCaps));
        Assert.AreEqual(bold | italic, CultureChoice.ComposeStyle(bold | italic, italic, false, 0));
        Assert.AreEqual(bold | italic | smallCaps, CultureChoice.ComposeStyle(bold | italic, italic, false, smallCaps));
        Assert.AreEqual(0, CultureChoice.ComposeStyle(0, italic, true, 0));
    }

    // D7 (Saleh 2026-10-20: "papers will change language and the pc background ... its a pc you set the language"): only a
    // paper's printed words, the newspaper and (our proposal) an in-world sign take the world's language; the PC and the
    // chrome (report, Home, citation, verdict, wheel, guide, memo, briefing) never do (BUG-3a).

    [TestCase(LabelLanguage.Culture)]
    [TestCase(LabelLanguage.SameAsReading)]
    [TestCase(LabelLanguage.NoTable)]
    [TestCase(LabelLanguage.EnglishBySetting)]
    [TestCase(LabelLanguage.EnglishNoFont)]
    public void Surface_ThePc_ReadsTheReadingLanguageWhateverThePapers(LabelLanguage paper)
    {
        Assert.AreEqual(LabelLanguage.EnglishOnPc, CultureChoice.Surface(TextSurface.Pc, paper));
        Assert.IsFalse(CultureChoice.ShowsCulture(CultureChoice.Surface(TextSurface.Pc, paper)));
    }

    [TestCase(LabelLanguage.Culture)]
    [TestCase(LabelLanguage.SameAsReading)]
    [TestCase(LabelLanguage.NoTable)]
    [TestCase(LabelLanguage.EnglishBySetting)]
    [TestCase(LabelLanguage.EnglishNoFont)]
    public void Surface_TheChrome_ReadsThePlayersLanguageWhateverThePapers(LabelLanguage paper)
    {
        Assert.AreEqual(LabelLanguage.PlayerLanguage, CultureChoice.Surface(TextSurface.Desk, paper));
        Assert.IsFalse(CultureChoice.ShowsCulture(CultureChoice.Surface(TextSurface.Desk, paper)));
        Assert.IsFalse(CultureChoice.FollowsWorld(TextSurface.Desk));
        Assert.IsFalse(CultureChoice.FollowsWorld(TextSurface.Pc));
    }

    [TestCase(LabelLanguage.Culture, true)]
    [TestCase(LabelLanguage.SameAsReading, false)]
    [TestCase(LabelLanguage.NoTable, false)]
    [TestCase(LabelLanguage.EnglishBySetting, false)]
    [TestCase(LabelLanguage.EnglishNoFont, false)]
    public void Surface_APaperAndASign_FollowTheCultureRule(LabelLanguage paper, bool culture)
    {
        foreach (TextSurface surface in new[] { TextSurface.Paper, TextSurface.Sign })
        {
            Assert.IsTrue(CultureChoice.FollowsWorld(surface), surface.ToString());
            Assert.AreEqual(paper, CultureChoice.Surface(surface, paper), surface.ToString());
            Assert.AreEqual(culture, CultureChoice.ShowsCulture(CultureChoice.Surface(surface, paper)), surface.ToString());
        }
    }

    [Test]
    public void Surface_OnTheCultureDayWithFollowHistory_OnlyThePapersReadTheCulture()
    {
        LabelLanguage paper = CultureChoice.Language("el", "en", true, CultureChoice.EnglishBySetting(CultureChoice.FollowHistoryChoice, true), true);
        Assert.AreEqual(LabelLanguage.Culture, paper);
        Assert.AreEqual(LabelLanguage.EnglishOnPc, CultureChoice.Surface(TextSurface.Pc, paper));
        Assert.AreEqual(LabelLanguage.PlayerLanguage, CultureChoice.Surface(TextSurface.Desk, paper));
        Assert.AreEqual(LabelLanguage.Culture, CultureChoice.Surface(TextSurface.Paper, paper));
    }
}
