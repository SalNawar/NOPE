using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

    [Test]
    public void TodaysFlavourLabels_AreThe28OfThePcRedesign()
    {
        List<string> flavour = FlavourKeys(TodaysUi());
        Assert.AreEqual(28, flavour.Count, string.Join(", ", flavour));
        foreach (string added in new[] { "icon.investigation", "icon.mail", "icon.account", "icon.settings" })
            CollectionAssert.Contains(flavour, added);
        foreach (string gone in new[] { "icon.directives", "icon.scanner", "icon.records", "icon.lexicon", "icon.dialect", "icon.material", "icon.clueLog", "window.directives", "window.scanner", "records.title" })
            CollectionAssert.DoesNotContain(flavour, gone);
    }

    [Test]
    public void EveryCultureLanguage_TranslatesExactlyTheFlavourLabels()
    {
        ContentNode ui = TodaysUi();
        List<string> flavour = FlavourKeys(ui);
        foreach (ContentNode language in ui.Get("languages").Items)
            CollectionAssert.AreEquivalent(flavour, language.Get("entries").Items.Select(e => e.Get("key").Text).ToList(), language.Get("language").Text);
    }
}
