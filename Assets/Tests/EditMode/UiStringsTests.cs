using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>The UI string lookup (piece 6 U7): fallbacks, number formats, glosses, shaping and the table rules; and today's flavour tables (the PC redesign TH3).</summary>
public class UiStringsTests
{
    private static UiStringEntry E(string key, string text, GlossStyle gloss = GlossStyle.None, StringTier tier = StringTier.Full) =>
        new UiStringEntry { key = key, text = text, gloss = gloss, tier = tier };

    private static readonly UiStringEntry[] Reading =
    {
        E("ok", "OK", GlossStyle.Below, StringTier.Flavour),
        E("same", "SAME", GlossStyle.Inline, StringTier.Flavour),
        E("plain", "Plain", GlossStyle.None, StringTier.Flavour),
        E("back", "< Office", GlossStyle.Below, StringTier.Flavour),
        E("day", "Day {0}", GlossStyle.None, StringTier.Flavour),
        E("pair", "{0} vs {1}"),
        E("full", "Only English")
    };

    private static readonly UiStringEntry[] Culture =
    {
        E("ok", "D'ACCORD"),
        E("same", "PAREIL"),
        E("plain", "Simple"),
        E("back", "< Bureau"),
        E("day", "Jour {0}")
    };

    private static UiStrings Strings(UiStringEntry[] culture = null, bool rtl = false) =>
        new UiStrings(Reading, culture, rtl, 60);

    private static string Codes(string s) => string.Join(" ", s.Select(c => ((int)c).ToString("X4")));

    [Test]
    public void Get_CultureHit_ThenReading_ThenTheKey()
    {
        UiStrings s = Strings(Culture);
        Assert.AreEqual("Simple", s.Get("plain"));
        Assert.AreEqual("Only English", s.Get("full"));
        Assert.AreEqual("nope", s.Get("nope"));
        Assert.AreEqual("nope", s.Get("nope"));
        CollectionAssert.AreEqual(new[] { "nope" }, s.MissingKeys.ToArray());
    }

    [Test]
    public void ABlankText_IsAbsent_TheCultureFallsBackToReading_TheReadingToTheKey()
    {
        // Unity serializes a null string as "": a blank entry must not draw a blank label (or a gloss alone).
        var reading = new List<UiStringEntry>(Reading) { E("empty", "") };
        var s = new UiStrings(reading, new[] { E("ok", ""), E("plain", "Simple") }, false, 60);
        Assert.AreEqual("OK", s.Get("ok"), "a blank culture entry falls back to the reading text, with no gloss");
        Assert.AreEqual("Simple", s.Get("plain"));
        Assert.AreEqual("empty", s.Get("empty"), "a blank reading entry is a missing key");
        CollectionAssert.AreEqual(new[] { "empty" }, s.MissingKeys.ToArray());
    }

    [Test]
    public void Format_FillsPlaceholders_AndPassesCanonicalStringsVerbatim()
    {
        UiStrings s = Strings();
        Assert.AreEqual("a vs b", s.Format("pair", "a", "b"));
        foreach (string canonical in new[] { "3 Jun 1450 BCE", "Babylon (Babili)", "Reichspräsident", "¥10,000" })
            Assert.AreEqual(canonical + " vs x", s.Format("pair", canonical, "x"));
        Assert.AreEqual(" vs x", s.Format("pair", null, "x"));
    }

    [Test]
    public void Format_AStringArgument_IgnoresItsSpecifier()
    {
        var s = new UiStrings(new[] { E("n", "{0:0.#}!") }, null, false, 60);
        Assert.AreEqual("abc!", s.Format("n", "abc"));
    }

    [TestCase("{0:+0;-0;0}", 5, "+5")]
    [TestCase("{0:+0;-0;0}", -3, "-3")]
    [TestCase("{0:+0;-0;0}", 0, "0")]
    [TestCase("{0:0}%", 87.4321f, "87%")]
    [TestCase("{0:+0.#;-0.#}", -2.26f, "-2.3")]
    public void Format_NumbersUseTheirSpecifier(string template, object value, string expected)
    {
        var s = new UiStrings(new[] { E("n", template) }, null, false, 60);
        Assert.AreEqual(expected, s.Format("n", value));
    }

    [Test]
    public void Format_NumbersIgnoreThePlayersCulture()
    {
        CultureInfo old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var s = new UiStrings(new[] { E("n", "{0:0.#}") }, null, false, 60);
            Assert.AreEqual("2.5", s.Format("n", 2.5f));
        }
        finally
        {
            CultureInfo.CurrentCulture = old;
        }
    }

    [Test]
    public void Glosses_BelowInlineAndNone()
    {
        UiStrings s = Strings(Culture);
        Assert.AreEqual("D'ACCORD\n<size=60%><noparse>OK</noparse></size>", s.Get("ok"));
        Assert.AreEqual("< Bureau\n<size=60%><noparse>< Office</noparse></size>", s.Get("back"));
        Assert.AreEqual("PAREIL (SAME)", s.Get("same"));
        Assert.AreEqual("Simple", s.Get("plain"));
    }

    [Test]
    public void AReadingHit_OrNoCultureTable_NeverGlosses()
    {
        Assert.AreEqual("OK", Strings().Get("ok"));
        Assert.AreEqual("OK", Strings(new[] { E("plain", "Simple") }).Get("ok"));
    }

    [Test]
    public void RightToLeft_ShapesTheCultureEntry_NotTheReadingFallback()
    {
        UiStrings s = Strings(new[] { E("day", "اليوم {0}") }, rtl: true);
        Assert.AreEqual("0033 00A0 FEE1 FEEE FEF4 FEDF FE8D", Codes(s.Format("day", 3)));
        Assert.AreEqual("Only English", s.Get("full"));
    }

    [Test]
    public void Placeholders_InFirstAppearanceOrder_WithSpecifiers()
    {
        CollectionAssert.AreEqual(new[] { "0", "1:0.#" }, UiStrings.Placeholders("Day {0} of {1:0.#}, {0}").ToArray());
        CollectionAssert.IsEmpty(UiStrings.Placeholders("{{literal}}"));
    }

    [Test]
    public void TableProblems_ACleanTableHasNone()
    {
        Assert.IsEmpty(UiStrings.TableProblems(Reading, Culture, false));
        Assert.IsEmpty(UiStrings.TableProblems(Reading, null, false));
    }

    [Test]
    public void TableProblems_ReportsEachFault()
    {
        var reading = new List<UiStringEntry>(Reading) { E("ok", "again"), E(" ", "blank"), E("brace", "Day {0"), E("stray", "x } y") };
        List<string> own = UiStrings.TableProblems(reading, null, false);
        Assert.IsTrue(own.Any(p => p.Contains("'ok'") && p.Contains("more than once")), string.Join(" | ", own));
        Assert.IsTrue(own.Any(p => p.Contains("blank key")));
        Assert.IsTrue(own.Any(p => p.Contains("'brace'") && p.Contains("unmatched")));
        Assert.IsTrue(own.Any(p => p.Contains("'stray'") && p.Contains("unmatched")));

        var culture = new[] { E("ghost", "x"), E("full", "Nur Deutsch"), E("day", "Tag {0:0}"), E("plain", "ٹ") };
        List<string> problems = UiStrings.TableProblems(Reading, culture, true);
        Assert.AreEqual(4, problems.Count, string.Join(" | ", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("'ghost'") && p.Contains("not in the reading table")));
        Assert.IsTrue(problems.Any(p => p.Contains("'full'") && p.Contains("flavour")));
        Assert.IsTrue(problems.Any(p => p.Contains("'day'") && p.Contains("placeholders")));
        Assert.IsTrue(problems.Any(p => p.Contains("'plain'") && p.Contains("U+0679")));
    }

    /// <summary>Today's ui block of world_source.json.</summary>
    private static ContentNode TodaysUi([CallerFilePath] string here = "")
    {
        const string source = "Assets/Data/World/world_source.json";
        string path = File.Exists(source) ? source : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", source);
        return ContentJson.Parse(File.ReadAllText(path)).Get("ui");
    }

    /// <summary>The flavour keys of today's reading table, in table order.</summary>
    private static List<string> FlavourKeys(ContentNode ui) =>
        ui.Get("strings").Items.Where(e => e.Get("tier").Text == "Flavour").Select(e => e.Get("key").Text).ToList();

    /// <summary>True when a text has letters outside its placeholders and rich-text tags (a label a culture translates).</summary>
    private static bool Worded(string text) =>
        Regex.Replace(Regex.Replace(text ?? string.Empty, "</?[A-Za-z#][^<>]*>", string.Empty), @"\{[^{}]*\}", string.Empty).Any(char.IsLetter);

    [Test]
    public void OnlyWordedLabels_AreFlavour_AndTheGamesNameIsNot()
    {
        // Saleh 2026-10-07: "I want the language to change on all documents and the apps": the labels with words are
        // translated (flavour); a key of placeholders and symbols only, and the game's name, stay as they are. A new
        // label may wait for its translations as Full (the validator lists those; the QA sweep's culture mode shows them).
        foreach (ContentNode e in TodaysUi().Get("strings").Items)
        {
            string key = e.Get("key").Text;
            if (e.Get("tier").Text == "Flavour")
                Assert.IsTrue(Worded(e.Get("text").Text) && key != "title.name", key);
        }
        int flavour = FlavourKeys(TodaysUi()).Count;
        Assert.GreaterOrEqual(flavour, 930, "the labels translated on 2026-10-08 stay translated");
    }

    /// <summary>The game's scripts folder (Assets/Scripts).</summary>
    private static string ScriptsFolder([CallerFilePath] string here = "")
    {
        const string scripts = "Assets/Scripts";
        return Directory.Exists(scripts) ? scripts : Path.Combine(Path.GetDirectoryName(here), "..", "..", "Scripts");
    }

    [Test]
    public void EveryLiteralKeyTheScriptsLookUp_IsInTheReadingTable()
    {
        // A key the code asks UiText for by a literal and the table lacks draws as the key itself (audit R4-023).
        // Keys built at run time (a prefix plus an id) are checked where they are made, not here.
        var keys = new HashSet<string>(TodaysUi().Get("strings").Items.Select(e => e.Get("key").Text));
        var literal = new Regex(@"UiText\.(?:Pc\.)?(?:Get|Format)\(\s*""([^""]+)""\s*[,)]");
        var missing = new List<string>();
        int found = 0;
        foreach (string file in Directory.GetFiles(ScriptsFolder(), "*.cs", SearchOption.AllDirectories))
            foreach (Match m in literal.Matches(File.ReadAllText(file)))
            {
                found++;
                if (!keys.Contains(m.Groups[1].Value))
                    missing.Add($"{m.Groups[1].Value} ({Path.GetFileName(file)})");
            }
        Assert.Greater(found, 100, "the scan finds the scripts' literal lookups");
        CollectionAssert.IsEmpty(missing, "keys looked up by a literal but missing from ui.strings");
    }

    [Test]
    public void EveryCultureLanguage_TranslatesExactlyTheFlavourLabels()
    {
        ContentNode ui = TodaysUi();
        List<string> flavour = FlavourKeys(ui);
        foreach (ContentNode language in ui.Get("languages").Items)
            CollectionAssert.AreEquivalent(flavour, language.Get("entries").Items.Select(e => e.Get("key").Text).ToList(), language.Get("language").Text);
    }

    // The PC is always in English (Saleh 2026-10-18: "language in the pc shouldn't change"): its lookup reads the reading table only.

    [Test]
    public void English_WithACultureTable_ReadsTheReadingText()
    {
        UiStrings s = Strings(Culture);
        Assert.AreEqual("Jour 3", s.Format("day", 3));
        Assert.AreEqual("Day 3", s.English("day", 3));
        Assert.AreEqual("OK", s.English("ok"));
    }

    [Test]
    public void English_RightToLeftCulture_IsNeitherShapedNorGlossed()
    {
        UiStrings s = Strings(new[] { E("ok", "قبول") }, true);
        Assert.AreEqual("OK", s.English("ok"));
    }

    [Test]
    public void English_AMissingKey_IsTheKeyAndIsReported()
    {
        UiStrings s = Strings(Culture);
        Assert.AreEqual("nope.key", s.English("nope.key"));
        CollectionAssert.Contains(s.MissingKeys, "nope.key");
    }

    [Test]
    public void English_RemembersNothingForTheLens()
    {
        UiStrings s = Strings(Culture);
        s.English("ok");
        Assert.AreEqual(0, s.Phrases.Count);
    }

    // TEXT-LANG (BUG-3a, D7): surfaces. Only a paper and a sign read the culture's labels; the chrome and the PC never do.

    /// <summary>A culture table of today's content as the theme service builds it (its entries, its direction, its words).</summary>
    private static UiStrings TodaysStrings(ContentNode ui, string language)
    {
        UiStringEntry[] Entries(IEnumerable<ContentNode> items) => items.Select(e => new UiStringEntry
        {
            key = e.Get("key").Text,
            text = e.Get("text").Text,
            gloss = e.Get("gloss") != null && e.Get("gloss").Text == "Below" ? GlossStyle.Below : e.Get("gloss") != null && e.Get("gloss").Text == "Inline" ? GlossStyle.Inline : GlossStyle.None,
            tier = e.Get("tier") != null && e.Get("tier").Text == "Flavour" ? StringTier.Flavour : StringTier.Full
        }).ToArray();
        ContentNode table = ui.Get("languages").Items.First(l => l.Get("language").Text == language);
        return new UiStrings(Entries(ui.Get("strings").Items), Entries(table.Get("entries").Items), table.Get("rtl").Text == "true", 60);
    }

    /// <summary>Surface routing for every UI key of today's content, in a left-to-right and a right-to-left culture: the chrome and the PC read the reading text, a paper and a sign the culture's label.</summary>
    [TestCase("el")]
    [TestCase("ar")]
    public void ForSurface_EveryKey_OnlyAPaperOrASignReadsTheCulture(string language)
    {
        ContentNode ui = TodaysUi();
        UiStrings s = TodaysStrings(ui, language);
        UiStrings reading = Strings();
        int cultured = 0;
        foreach (ContentNode e in ui.Get("strings").Items)
        {
            string key = e.Get("key").Text;
            object[] args = { "A", "B", "C", "D" };
            string english = s.English(key, args);
            Assert.AreEqual(english, s.ForSurface(TextSurface.Desk, key, args), key + " on the chrome");
            Assert.AreEqual(english, s.ForSurface(TextSurface.Pc, key, args), key + " on the PC");
            Assert.AreEqual(s.Format(key, args), s.ForSurface(TextSurface.Paper, key, args), key + " on a paper");
            Assert.AreEqual(s.Format(key, args), s.ForSurface(TextSurface.Sign, key, args), key + " on a sign");
            if (s.ForSurface(TextSurface.Paper, key, args) != english)
                cultured++;
        }
        Assert.Greater(cultured, 900, "the papers do read the culture's labels");
        Assert.AreEqual("nope.key", reading.ForSurface(TextSurface.Desk, "nope.key"), "a missing key is the key on any surface");
    }

    /// <summary>CODE-44: an argument already shaped is turned back into logical text, so a right-to-left label is shaped once, at the outermost format, and reads the same as with the logical argument.</summary>
    [Test]
    public void Format_RightToLeft_AShapedArgument_IsShapedOnce()
    {
        UiStrings s = Strings(new[] { E("day", "اليوم {0}"), E("pair", "{0} مقابل {1}") }, rtl: true);
        string word = "الأدلة";
        Assert.AreEqual(s.Format("day", word), s.Format("day", ArabicShaper.ToVisual(word)));
        Assert.AreEqual(s.Format("pair", word, "سجل"), s.Format("pair", ArabicShaper.ToVisual(word), ArabicShaper.ToVisual("سجل")));
        Assert.AreEqual(ArabicShaper.ToVisual("اليوم " + word), s.Format("day", ArabicShaper.ToVisual(word)), "the whole label in visual order, the argument read in place");
    }

    // TEXT-LANG (BUG-3a; CODE-45, CODE-79; FIX_PLAN TEXT-LANG Tests: "records ... a round trip in another language
    // renders correctly"): a citation, Mail's copy and the visit record are built on the chrome from a paper's word
    // (CaseFactory: "{0} on the {1}" with UiText.DocumentWord). On the culture day the word on the paper is the
    // culture's; the record must still hold none of the world's language.

    /// <summary>The culture labels a lookup has printed so far, as the takeover grep reads them (TextLangAudit).</summary>
    private static IEnumerable<(string, string)> Shown(UiStrings s) =>
        s.Phrases.SelectMany(p => new[] { (p.Visual, p.English), (p.Logical, p.English) });

    /// <summary>
    /// Every paper word of today's content, printed on the culture day (UiText.FormWords: PaperWord) and then quoted in
    /// a citation's value label as CaseFactory builds it (citation.label.onPaper, citation.label.book) on the chrome and
    /// on the PC: the label is exactly the English one, and the takeover grep finds no culture label in it, while the
    /// same label on a paper keeps the culture's word.
    /// </summary>
    [TestCase("el")]
    [TestCase("ar")]
    [TestCase("zh-Hans")]
    public void ACitationBuiltFromAPapersWord_HoldsNoWorldLanguage_OnTheCultureDay(string language)
    {
        ContentNode ui = TodaysUi();
        UiStrings s = TodaysStrings(ui, language);
        var records = new List<(string, TextSurface)>();
        int cultured = 0;
        foreach (ContentNode e in ui.Get("strings").Items.Where(e => e.Get("key").Text.StartsWith(DocumentWords.Prefix)))
        {
            string english = e.Get("text").Text; // as the content prints it (a paper's label, an office's name)
            string word = s.PaperWord(DocumentWords.Key(english), english);
            if (word != english)
                cultured++;
            string onPaper = s.ForSurface(TextSurface.Desk, "citation.label.onPaper", word, "Passport");
            Assert.AreEqual(s.English("citation.label.onPaper", english, "Passport"), onPaper, e.Get("key").Text + " in a citation");
            string book = s.ForSurface(TextSurface.Pc, "citation.label.book", s.English("category.Seal"), word);
            Assert.AreEqual(s.English("citation.label.book", s.English("category.Seal"), english), book, e.Get("key").Text + " in Mail's copy");
            records.Add((onPaper, TextSurface.Desk));
            records.Add((book, TextSurface.Pc));
            StringAssert.Contains(word, s.ForSurface(TextSurface.Paper, "citation.label.onPaper", word, "Passport"), "a paper keeps the culture's word");
        }
        Assert.Greater(cultured, 150, "the papers do print the culture's words");
        CollectionAssert.IsEmpty(TextAudit.CultureOnPlayerSurfaces(records, Shown(s)), "the takeover grep over the records");
    }

    /// <summary>A record built on the culture day reads the same as one built before it (the round trip: it holds the player's language, whatever the papers printed).</summary>
    [Test]
    public void ACitationRecord_ReadsTheSame_BeforeAndOnTheCultureDay()
    {
        UiStrings s = TodaysStrings(TodaysUi(), "el");
        const string label = "SURNAME / GIVEN NAMES";
        string week1 = s.ForSurface(TextSurface.Desk, "citation.label.onPaper", label, "Passport"); // before the culture day the paper prints its own English
        string word = s.PaperWord(DocumentWords.Key(label), label);
        Assert.AreNotEqual(label, word, "the paper reads Greek on the culture day");
        Assert.AreEqual(week1, s.ForSurface(TextSurface.Desk, "citation.label.onPaper", word, "Passport"));
        Assert.AreEqual("SURNAME / GIVEN NAMES on the Passport", week1);
    }

    /// <summary>A culture label (not only a paper word) passed into a chrome or PC string prints as its English; other arguments, numbers and canonical values pass verbatim.</summary>
    [Test]
    public void English_ACultureLabelArgument_PrintsItsEnglish()
    {
        UiStrings s = Strings(Culture);
        string culture = s.Format("plain");
        Assert.AreEqual("Simple", culture);
        Assert.AreEqual("Plain vs Babylon (Babili)", s.English("pair", culture, "Babylon (Babili)"));
        Assert.AreEqual("Plain vs 3", s.ForSurface(TextSurface.Pc, "pair", culture, 3));
        Assert.AreEqual("Simple vs x", s.ForSurface(TextSurface.Paper, "pair", culture, "x"), "a paper keeps it");
    }

    /// <summary>FIX_PLAN_v2 ("Paper: documents and the newspaper"): the newspaper's own words, looked up the chrome's way, read the culture's language on the culture day; the Bureau memo's line stays the chrome's.</summary>
    [TestCase("el")]
    [TestCase("ar")]
    public void TheNewspapersWords_ReadTheCulture_TheMemoDoesNot(string language)
    {
        ContentNode ui = TodaysUi();
        UiStrings s = TodaysStrings(ui, language);
        foreach (string key in CultureChoice.NewspaperKeys)
        {
            string shown = s.ForSurface(CultureChoice.SurfaceOfKey(key, TextSurface.Desk), key, 8);
            Assert.AreEqual(s.Format(key, 8), shown, key);
            Assert.AreNotEqual(s.English(key, 8), shown, key + " is translated");
        }
        Assert.AreEqual(s.English("briefing.newHours", "08:00", "17:00"),
                        s.ForSurface(CultureChoice.SurfaceOfKey("briefing.newHours", TextSurface.Desk), "briefing.newHours", "08:00", "17:00"));
    }
}
