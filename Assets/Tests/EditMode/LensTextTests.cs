using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

/// <summary>The Translation Lens's text (Saleh 2026-10-06): a culture label's words and their English, the letter flip between texts of any length and script, right to left included.</summary>
public class LensTextTests
{
    private static Dictionary<string, string> Glossary(params string[] pairs)
    {
        var words = new List<LensWord>();
        for (int i = 0; i + 1 < pairs.Length; i += 2)
            words.Add(new LensWord { native = pairs[i], english = pairs[i + 1] });
        return LensWords.Glossary(words);
    }

    private static readonly FlipTiming Timing = new FlipTiming { startDelay = 0f, letterInterval = 0.1f, letterSeconds = 0.1f, scrambleSteps = 1 };

    [Test]
    public void Split_ArabicLabel_WordsAndTheRest()
    {
        var g = Glossary("اليوم", "Day", "الإحاطة", "Briefing", "الصباحية", "Morning");
        List<LensSegment> s = LensWords.Split("اليوم 8 — الإحاطة الصباحية", "Day 8 — Morning Briefing", g);
        var words = s.FindAll(x => x.IsWord).ConvertAll(x => x.English);
        CollectionAssert.AreEqual(new[] { "Day", "Briefing", "Morning" }, words);
        Assert.IsFalse(s.Exists(x => !x.IsWord && x.English != null), "the digit and the dash are never words");
    }

    [Test]
    public void Split_Japanese_CutAtTheGlossaryWords_LongestFirst()
    {
        var g = Glossary("朝", "morning", "朝の", "Morning", "ブリーフィング", "Briefing", "日目", "day");
        List<LensSegment> s = LensWords.Split("8日目 — 朝のブリーフィング", "Day 8 — Morning Briefing", g);
        var words = s.FindAll(x => x.IsWord).ConvertAll(x => x.English);
        CollectionAssert.AreEqual(new[] { "day", "Morning", "Briefing" }, words, "朝の wins over 朝");
    }

    [Test]
    public void Split_UnknownUnspacedLetters_StayOneWordWithNoEnglish()
    {
        var g = Glossary("ブリーフィング", "Briefing");
        List<LensSegment> s = LensWords.Split("朝のブリーフィング", "Morning Briefing", g);
        Assert.AreEqual(2, s.Count);
        Assert.AreEqual(2, s[0].Length);
        Assert.IsNull(s[0].English);
        CollectionAssert.AreEqual(new[] { "朝の" }, LensWords.Missing("朝のブリーフィング", "Morning Briefing", g));
    }

    [Test]
    public void Split_IgnoresCaseAndAccents_AndCapitalisesForACapitalLabel()
    {
        var g = Glossary("Έναρξη", "start", "βάρδιας", "shift");
        List<LensSegment> s = LensWords.Split("ΕΝΑΡΞΗ ΒΑΡΔΙΑΣ", "START SHIFT", g);
        CollectionAssert.AreEqual(new[] { "START", "SHIFT" }, s.FindAll(x => x.IsWord).ConvertAll(x => x.English));
    }

    [Test]
    public void Split_ASingleWordLabel_TakesTheLabelsEnglish()
    {
        List<LensSegment> s = LensWords.Split("الإعدادات", "Settings", Glossary());
        Assert.AreEqual("Settings", s[0].English);
        List<LensSegment> day = LensWords.Split("اليوم 8", "Day 8", Glossary());
        Assert.IsNull(day[0].English, "a label with a number besides its word needs the glossary (no 'Day 8 8')");
    }

    [Test]
    public void Split_ARichTextTag_IsNeverAWord()
    {
        var g = LensWords.Glossary(new List<LensWord> { new LensWord { native = "Dati", english = "Data" } });
        List<LensSegment> s = LensWords.Split("<b>Dati</b> <pos=76%>x < y", "<b>Data</b>", g);

        Assert.AreEqual(new[] { "Dati", "x", "y" }, s.FindAll(x => x.IsWord).ConvertAll(x => "<b>Dati</b> <pos=76%>x < y".Substring(x.Start, x.Length)).ToArray());
        Assert.AreEqual("Data", s.Find(x => x.IsWord).English);
        Assert.AreEqual("Data", LensWords.Split("<b>Dati</b>", "<b>Data</b>", null).Find(x => x.IsWord).English, "a tagged single word still takes the label's English");
    }

    [Test]
    public void TableProblems_NameTheWordsWithNoEnglish_AndBadEntries()
    {
        var reading = new List<UiStringEntry> { new UiStringEntry { key = "tray.day", text = "Day {0}" }, new UiStringEntry { key = "icon.mail", text = "Mail" } };
        var culture = new List<UiStringEntry> { new UiStringEntry { key = "tray.day", text = "اليوم {0}" }, new UiStringEntry { key = "icon.mail", text = "البريد" } };
        List<string> problems = LensWords.TableProblems(reading, culture, new List<LensWord>());
        Assert.AreEqual(1, problems.Count, string.Join("\n", problems));
        StringAssert.Contains("'اليوم'", problems[0]);

        var words = new List<LensWord> { new LensWord { native = "اليوم", english = "Day" }, new LensWord { native = "اليوم", english = "Today" }, new LensWord { native = "x", english = "" } };
        Assert.AreEqual(2, LensWords.TableProblems(reading, culture, words).Count, "a repeat and a blank English");
    }

    [Test]
    public void Phrase_Compose_ReplacesAWord_RightToLeftShapedAndMapped()
    {
        var phrase = new LensPhrase("سجل المناوبة", "Shift Ledger", true, Glossary("سجل", "Ledger", "المناوبة", "Shift"));
        Assert.AreEqual(2, phrase.WordCount);
        Assert.AreEqual(ArabicShaper.ToVisual("سجل المناوبة"), phrase.Visual);

        var map = new List<int>();
        string shown = phrase.Compose(new[] { "Ledger", null }, map);
        StringAssert.Contains("Ledger", shown, "the English word reads left to right inside the right-to-left line");
        Assert.AreEqual(shown.Length, map.Count);
        int at = shown.IndexOf('L');
        Assert.AreEqual(0, phrase.WordRank(map[at]), "its characters map back to the first word");
    }

    [Test]
    public void Phrase_ComposeWhole_AnEnglishSentenceReadsLeftToRight()
    {
        var phrase = new LensPhrase("سجل المناوبة", "Shift Ledger", true, Glossary());
        var map = new List<int>();
        Assert.AreEqual("Shift Ledger", phrase.ComposeWhole("Shift Ledger", map));
        Assert.IsTrue(map.TrueForAll(m => m == -1));
    }

    [Test]
    public void Flip_LettersLandOneByOne_InReadingOrder()
    {
        Assert.AreEqual("البريد", LensFlip.Frame("البريد", "Mail", Timing, float.NaN), "before the start: the native word");
        Assert.AreEqual("Mail", LensFlip.Frame("البريد", "Mail", Timing, float.PositiveInfinity), "reduced motion: at once");
        string mid = LensFlip.Frame("البريد", "Mail", Timing, 0.25f);
        Assert.AreEqual("Ma", mid.Substring(0, 2), "two cells landed");
        Assert.AreEqual(6, mid.Length, "cell 2 scrambling, cells 3 to 5 still native (the longer word sets the cells)");
        Assert.AreEqual("Mail", LensFlip.Frame("البريد", "Mail", Timing, LensFlip.Duration("البريد", "Mail", Timing)));
    }

    [Test]
    public void Flip_ToALongerText_GrowsAsCellsLand()
    {
        Assert.AreEqual(4, LensFlip.Cells("設定", "Sett"));
        Assert.AreEqual("設定", LensFlip.Frame("設定", "Settings", Timing, -1f));
        Assert.AreEqual("Settings", LensFlip.Frame("設定", "Settings", Timing, 10f));
        string mid = LensFlip.Frame("設定", "Settings", Timing, 0.35f);
        StringAssert.StartsWith("Set", mid);
        Assert.AreEqual(4, mid.Length, "three landed and one scrambling; the rest not yet there");
    }

    [Test]
    public void Flip_ScrambleUsesTheLeftWordsLetters()
    {
        var sb = new StringBuilder();
        LensFlip.Frame("設定", "Settings", new FlipTiming { startDelay = 0f, letterInterval = 0f, letterSeconds = 1f, scrambleSteps = 2 }, 0.1f, sb);
        foreach (char c in sb.ToString())
            Assert.IsTrue(c == '設' || c == '定', $"'{c}' is a letter of the word it leaves");
    }

    [Test]
    public void UiStrings_RemembersEveryCultureLabelItShows()
    {
        var reading = new List<UiStringEntry> { new UiStringEntry { key = "icon.mail", text = "Mail", tier = StringTier.Flavour }, new UiStringEntry { key = "tray.day", text = "Day {0}", tier = StringTier.Flavour } };
        var culture = new List<UiStringEntry> { new UiStringEntry { key = "icon.mail", text = "メール" }, new UiStringEntry { key = "tray.day", text = "{0}日目" } };
        var strings = new UiStrings(reading, culture, false, 60, new List<LensWord> { new LensWord { native = "日目", english = "day" } });
        Assert.AreEqual("メール", strings.Get("icon.mail"));
        Assert.AreEqual("8日目", strings.Format("tray.day", 8));
        strings.Get("icon.mail");
        Assert.AreEqual(2, strings.Phrases.Count, "each label once");
        Assert.AreEqual("Day 8", strings.Phrases[1].English);
        Assert.AreEqual("day", strings.Phrases[1].EnglishWord(0));
        Assert.AreEqual(0, new UiStrings(reading, null, false, 60).Phrases.Count, "English labels need no lens");
    }
}
