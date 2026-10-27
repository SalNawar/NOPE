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

    /// <summary>BUG-3a, D7 ("you set the language"; culture effects "only start to impact the game after day 7"), R2 ("become visible from day 8"): the setting never locks. Always English keeps the papers English on any day; Follow history, like no choice, reads English until the culture day (the lens's day) and the world's language from then on, never before.</summary>
    [TestCase(null, false, true, Description = "week 1, never chosen: English")]
    [TestCase(null, true, false, Description = "the culture day, never chosen: the world's language")]
    [TestCase("english", false, true)]
    [TestCase("english", true, true, Description = "never locked: Always English still holds on the culture day")]
    [TestCase("history", false, true, Description = "week 1, Follow history: English until the culture day (D7, R2)")]
    [TestCase("history", true, false)]
    [TestCase("", true, false, Description = "an unknown value reads as no choice")]
    [TestCase("", false, true)]
    [TestCase("English", true, false, Description = "ordinal: only the stored value itself keeps English")]
    public void EnglishBySetting_NeverLocks(string stored, bool cultureDay, bool expected)
    {
        Assert.AreEqual(expected, CultureChoice.EnglishBySetting(stored, cultureDay));
    }

    [Test]
    public void Language_DayTwoAfterACultureLeads_EnglishByDefault_CultureFromTheLensDay_UnlessAlwaysEnglish()
    {
        // Saleh's playtest of demo 1008a: day 1's decision made Greece lead and day 2 read Greek with no lens.
        Assert.AreEqual(LabelLanguage.EnglishBySetting, CultureChoice.Language("el", "en", true, CultureChoice.EnglishBySetting(null, false), true));
        Assert.AreEqual(LabelLanguage.EnglishBySetting, CultureChoice.Language("el", "en", true, CultureChoice.EnglishBySetting(CultureChoice.FollowHistoryChoice, false), true),
                        "Follow history waits for the culture day too (D7, R2)");
        Assert.AreEqual(LabelLanguage.Culture, CultureChoice.Language("el", "en", true, CultureChoice.EnglishBySetting(null, true), true));
        Assert.AreEqual(LabelLanguage.EnglishBySetting, CultureChoice.Language("el", "en", true, CultureChoice.EnglishBySetting(CultureChoice.AlwaysEnglishChoice, true), true),
                        "the setting never locks (BUG-3a)");
    }

    /// <summary>The Settings pair shows the stored choice, not today's effect: Follow history (or no choice) stays chosen before the culture day.</summary>
    [TestCase(null, false)]
    [TestCase("history", false)]
    [TestCase("english", true)]
    [TestCase("English", false)]
    public void ChoseAlwaysEnglish_IsTheStoredChoice(string stored, bool expected)
    {
        Assert.AreEqual(expected, CultureChoice.ChoseAlwaysEnglish(stored));
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
    // paper's printed words, the newspaper's own words (CultureChoice.NewspaperKeys) and (our proposal) an in-world sign
    // take the world's language; the PC and the chrome (report, Home, citation, verdict, wheel, guide, the Bureau memo)
    // never do (BUG-3a).

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

    /// <summary>FIX_PLAN_v2 TEXT-LANG ("Paper: documents and the newspaper"), D7 (the glasses work "on news paper and documents"): the newspaper's own words (its masthead, edition line and price as well as its dateline and heads) print on the Paper surface even when looked up through the chrome's lookup; the Bureau memo's and every other chrome key stay the chrome's; a key asked for on another surface stays there.</summary>
    [Test]
    public void SurfaceOfKey_TheNewspapersWords_AreAPapers()
    {
        foreach (string key in new[] { "briefing.masthead", "paper.edition", "paper.price", "briefing.title", "briefing.empty", "briefing.newsHeader", "briefing.deskHeader" })
        {
            Assert.IsTrue(CultureChoice.IsNewspaperKey(key), key);
            Assert.AreEqual(TextSurface.Paper, CultureChoice.SurfaceOfKey(key, TextSurface.Desk), key);
            Assert.AreEqual(TextSurface.Pc, CultureChoice.SurfaceOfKey(key, TextSurface.Pc), key + " asked for on the PC stays the PC's");
        }
        Assert.AreEqual(7, CultureChoice.NewspaperKeys.Length, "the masthead, edition line, price, dateline, \"no news\" line and the two story heads");
        foreach (string key in new[] { "briefing.newHours", "citation.label.onPaper", "list.bullet", "settings.language", null, "" })
        {
            Assert.IsFalse(CultureChoice.IsNewspaperKey(key), key ?? "null");
            Assert.AreEqual(TextSurface.Desk, CultureChoice.SurfaceOfKey(key, TextSurface.Desk), key ?? "null");
        }
        Assert.AreEqual(TextSurface.Sign, CultureChoice.SurfaceOfKey("accept", TextSurface.Sign));
    }
}
