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

    [TestCase(null, true, Description = "never chosen: English (the default before the lens's day)")]
    [TestCase("", true)]
    [TestCase("english", true)]
    [TestCase("history", false, Description = "opted into Follow history")]
    [TestCase("History", true, Description = "ordinal: only the stored value itself opts in")]
    public void ChoseEnglish_UnlessThePlayerOptedIntoHistory(string stored, bool expected)
    {
        Assert.AreEqual(expected, CultureChoice.ChoseEnglish(stored));
    }

    [Test]
    public void EnglishBySetting_BeforeTheLensDayUnlessOptedIn_NeverOnceLocked()
    {
        Assert.IsTrue(CultureChoice.EnglishBySetting(null, false), "week 1, never chosen: English");
        Assert.IsTrue(CultureChoice.EnglishBySetting(CultureChoice.AlwaysEnglishChoice, false));
        Assert.IsFalse(CultureChoice.EnglishBySetting(CultureChoice.FollowHistoryChoice, false), "week 1, opted in: history");
        Assert.IsFalse(CultureChoice.EnglishBySetting(null, true), "the lens's day: history whatever the choice");
        Assert.IsFalse(CultureChoice.EnglishBySetting(CultureChoice.AlwaysEnglishChoice, true));
        Assert.IsFalse(CultureChoice.EnglishBySetting(CultureChoice.FollowHistoryChoice, true));
    }

    [Test]
    public void Language_DayTwoAfterACultureLeads_EnglishByDefault_CultureFromTheLensDay()
    {
        // Saleh's playtest of demo 1008a: day 1's decision made Greece lead and day 2 read Greek with no lens.
        Assert.AreEqual(LabelLanguage.EnglishBySetting, CultureChoice.Language("el", "en", true, CultureChoice.EnglishBySetting(null, false), true));
        Assert.AreEqual(LabelLanguage.Culture, CultureChoice.Language("el", "en", true, CultureChoice.EnglishBySetting(CultureChoice.FollowHistoryChoice, false), true));
        Assert.AreEqual(LabelLanguage.Culture, CultureChoice.Language("el", "en", true, CultureChoice.EnglishBySetting(null, true), true));
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
}
