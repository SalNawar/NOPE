using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// Arabic shaping and visual order for TextMeshPro (piece 6 U9, R6). Golden
/// cases computed with the reference model
/// docs/superpowers/drafts/piece6-support/shaper.py.
/// </summary>
public class ArabicShaperTests
{
    private static string Codes(string s) => string.Join(" ", s.Select(c => ((int)c).ToString("X4")));

    [TestCase("قبول", "FEDD FEEE FE92 FED7")]
    [TestCase("رفض", "FEBE FED3 FEAD")]
    [TestCase("لا", "FEFB", Description = "lam-alef")]
    [TestCase("الأدلة", "FE94 FEDF FEA9 FEF7 FE8D", Description = "a lam-alef with hamza after a right-joining alef")]
    [TestCase("إلى", "FEF0 FEDF FE87")]
    [TestCase("الاستقرار: 87%", "0038 0037 0025 00A0 003A FEAD FE8D FEAE FED8 FE98 FEB3 FEFB FE8D", Description = "the percent stays with its digits")]
    [TestCase("عدم تطابق", "FED6 FE91 FE8E FEC4 FE97 00A0 FEE1 FEAA FECB", Description = "the space becomes no-break")]
    [TestCase("سجل الأدلة", "FE94 FEDF FEA9 FEF7 FE8D 00A0 FEDE FEA0 FEB3")]
    [TestCase("(اليوم)", "0028 FEE1 FEEE FEF4 FEDF FE8D 0029", Description = "brackets are mirrored")]
    [TestCase("اليوم 3", "0033 00A0 FEE1 FEEE FEF4 FEDF FE8D")]
    [TestCase("شيء", "FE80 FEF2 FEB7", Description = "a hamza after a joining letter stays isolated (it has no final form)")]
    [TestCase("كء", "FE80 FED9", Description = "a letter before a hamza does not join it")]
    public void ToVisual_GoldenCases(string logical, string expected)
    {
        Assert.AreEqual(expected, Codes(ArabicShaper.ToVisual(logical)));
    }

    [Test]
    public void ToVisual_LinesKeepTheirOrder_EachShapedOnItsOwn()
    {
        // TMP draws a line break where it lies: each line goes into visual order by itself, the first line staying first (the 1018 language pass: a two-line Arabic label read bottom line first).
        string first = "قبول", second = "رفض";
        Assert.AreEqual(ArabicShaper.ToVisual(first) + "\n" + ArabicShaper.ToVisual(second), ArabicShaper.ToVisual(first + "\n" + second));
    }

    [Test]
    public void ToVisual_AcrossLines_ReportsEachCharactersLogicalSource()
    {
        string logical = "قبول\nرفض";
        var sources = new List<int>();
        string visual = ArabicShaper.ToVisual(logical, sources);
        Assert.AreEqual(visual.Length, sources.Count);
        Assert.AreEqual(4, sources[visual.IndexOf('\n')]);
        Assert.IsTrue(sources.Take(visual.IndexOf('\n')).All(i => i < 4));
        Assert.IsTrue(sources.Skip(visual.IndexOf('\n') + 1).All(i => i > 4));
    }

    [TestCase("ACCEPT 1", Description = "Latin keeps its order and its plain space")]
    [TestCase("批准")]
    [TestCase("")]
    public void ToVisual_TextWithoutArabic_IsUnchanged(string text)
    {
        Assert.AreEqual(text, ArabicShaper.ToVisual(text));
    }

    [Test]
    public void ToVisual_AHamzaAfterAnyLetter_IsNeverAMissingForm()
    {
        foreach (char letter in "ءآأؤإئابةتثجحخدذرزسشصضطظعغفقكلمنهوىي")
        {
            string visual = ArabicShaper.ToVisual(letter.ToString() + "ء" + letter);
            Assert.IsFalse(visual.Contains('\0'), $"'{letter}' then hamza: {Codes(visual)}");
            Assert.AreEqual(3, visual.Length, letter.ToString());
        }
    }

    /// <summary>Audit R2-001: the visual order reports each character's logical source, so a typed line can show its characters in reading order.</summary>
    [Test]
    public void ToVisual_ReportsEachVisualCharactersLogicalSource()
    {
        var sources = new List<int> { 99 };
        string visual = ArabicShaper.ToVisual("لا 12 ب", sources);
        Assert.AreEqual(ArabicShaper.ToVisual("لا 12 ب"), visual);
        // Logical: lam 0, alef 1, space 2, '1' 3, '2' 4, space 5, beh 6. Visual: beh, space, 1, 2, space, the lam-alef ligature (its lam's index).
        CollectionAssert.AreEqual(new[] { 6, 5, 3, 4, 2, 0 }, sources);

        ArabicShaper.ToVisual("ACCEPT 1", sources);
        CollectionAssert.AreEqual(Enumerable.Range(0, 8).ToArray(), sources, "no Arabic: each character is its own source");
        ArabicShaper.ToVisual(null, sources);
        Assert.IsEmpty(sources);
    }

    /// <summary>Audit R2-022: Latin and digits inside Arabic keep their own order, each run where the right-to-left reading puts it.</summary>
    [TestCase("ب Baghdad ت", "FE95 00A0 0042 0061 0067 0068 0064 0061 0064 00A0 FE8F", Description = "a Latin word between two Arabic ones")]
    [TestCase("ب Abbasid Baghdad 12 ت", "FE95 00A0 0041 0062 0062 0061 0073 0069 0064 0020 0042 0061 0067 0068 0064 0061 0064 0020 0031 0032 00A0 FE8F", Description = "Latin and digits in one run, their plain spaces kept")]
    [TestCase("ب Abbasid Baghdad (Medieval).", "002E 0041 0062 0062 0061 0073 0069 0064 0020 0042 0061 0067 0068 0064 0061 0064 0020 0028 004D 0065 0064 0069 0065 0076 0061 006C 0029 00A0 FE8F", Description = "a bracketed Latin label keeps its brackets; the full stop ends the line at its left")]
    [TestCase("ب (Rome) ت", "FE95 00A0 0028 0052 006F 006D 0065 0029 00A0 FE8F", Description = "Latin in brackets after Arabic: the brackets read right to left around it")]
    [TestCase("Rome (ب) Paris", "0050 0061 0072 0069 0073 00A0 0028 FE8F 0029 00A0 0052 006F 006D 0065", Description = "Arabic in brackets: the brackets keep the paragraph's direction")]
    public void ToVisual_LatinInsideArabic_KeepsItsOrder(string logical, string expected)
    {
        Assert.AreEqual(expected, Codes(ArabicShaper.ToVisual(logical)));
    }

    /// <summary>A Latin span inside a right-to-left line (piece 9's key words) reads as one run: all of it, in order, brackets included.</summary>
    [Test]
    public void ToVisual_ALatinSpanInsideArabic_StaysWhole()
    {
        const string span = "home to Abbasid Baghdad (Medieval)";
        StringAssert.Contains(span, ArabicShaper.ToVisual("ا ب " + span + "."));
    }

    [Test]
    public void ToVisual_Null_IsEmpty()
    {
        Assert.AreEqual(string.Empty, ArabicShaper.ToVisual(null));
    }

    [Test]
    public void CanShape_NamesCharactersTheTablesLack()
    {
        Assert.IsFalse(ArabicShaper.CanShape("ٹ", out string unsupported));
        StringAssert.Contains("U+0679", unsupported);
        foreach (string w in new[] { "قبول", "رفض", "لا", "الأدلة", "إلى", "الاستقرار: 87%", "عدم تطابق", "سجل الأدلة", "(اليوم)" })
            Assert.IsTrue(ArabicShaper.CanShape(w, out _), w);
    }

    /// <summary>Audit R2-022: the Arabic comma, question mark and digits are outside the tables (they would be reversed), so no table may use them.</summary>
    [TestCase("،", "U+060C")]
    [TestCase("؟", "U+061F")]
    [TestCase("١", "U+0661")]
    public void CanShape_RejectsArabicPunctuationAndDigits(string text, string code)
    {
        Assert.IsFalse(ArabicShaper.CanShape(text, out string unsupported));
        StringAssert.Contains(code, unsupported);
    }

    /// <summary>BUG-37a / CODE-42: the per-line API is ToVisual on each line, but its lines keep their plain spaces (they are laid out already; only a box that wraps a visual string needs no-break ones).</summary>
    [Test]
    public void ToVisualLines_IsToVisualOnEachLine_WithPlainSpaces()
    {
        var lines = new List<string> { "مرحبا بالعالم", "Desk 3", "رقم 42", string.Empty };
        List<string> visual = ArabicShaper.ToVisualLines(lines);
        Assert.AreEqual(lines.Count, visual.Count);
        for (int i = 0; i < lines.Count; i++)
            Assert.AreEqual(ArabicShaper.ToVisual(lines[i]).Replace('\u00A0', ' '), visual[i]);
        Assert.IsFalse(visual.Any(v => v.Contains('\u00A0')), "no no-break space in a laid-out line");
        Assert.AreEqual(0, ArabicShaper.ToVisualLines(null).Count);
    }

    // ---------------------------------------------------------------------
    // TEXT-LANG (BUG-37a): tags, shape once, wrap then reorder, mixed runs
    // ---------------------------------------------------------------------

    /// <summary>The tag names the tests read (the test's own reading of TMP's tags).</summary>
    private const string TestTags = "b|i|u|s|size|color|noparse|sup|sub|mark|alpha|font|nobr|link|style";

    /// <summary>A text's visible characters: its tags taken out.</summary>
    private static string Visible(string text) => Regex.Replace(text, "</?(" + TestTags + ")(=[^<>]*)?>", string.Empty);

    /// <summary>True when every opening tag of <paramref name="text"/> is closed after it, innermost first (TMP's nesting).</summary>
    private static bool Balanced(string text)
    {
        var open = new Stack<string>();
        foreach (Match m in Regex.Matches(text, "<(/?)(" + TestTags + ")(=[^<>]*)?>"))
        {
            if (m.Groups[1].Value.Length == 0)
                open.Push(m.Groups[2].Value);
            else if (open.Count == 0 || open.Pop() != m.Groups[2].Value)
                return false;
        }
        return open.Count == 0;
    }

    /// <summary>CODE-43: a tag is a token without a direction: the letters reorder around it, it never mirrors ("b&gt;" or "&lt;/b" never show) and the bold stays on its words.</summary>
    [TestCase("<b>صورته واسمه</b> ب", "صورته واسمه")]
    [TestCase("ب <b>قبول</b>", "قبول")]
    [TestCase("<b>الدينار</b>", "الدينار")]
    public void ToVisual_TagsAreTokens_BalancedAroundTheirWords(string logical, string bold)
    {
        string visual = ArabicShaper.ToVisual(logical);
        Assert.IsTrue(Balanced(visual), visual);
        StringAssert.Contains("<b>" + ArabicShaper.ToVisual(bold) + "</b>", visual, "the bold run, shaped and reversed, inside its own tags");
        Assert.AreEqual(ArabicShaper.ToVisual(Regex.Replace(logical, "</?b>", string.Empty)), Visible(visual), "the letters as without the tags");
        Assert.IsFalse(Regex.IsMatch(Visible(visual), "[<>]"), "no stray tag character");
    }

    /// <summary>Nested and valued tags (a gloss's size, a colour) stay balanced and keep their values.</summary>
    [Test]
    public void ToVisual_NestedAndValuedTags_StayBalanced()
    {
        string logical = "أ <size=60%><color=#FF0000>ب ت</color> ث</size> ج";
        string visual = ArabicShaper.ToVisual(logical);
        Assert.IsTrue(Balanced(visual), visual);
        StringAssert.Contains("<size=60%>", visual);
        StringAssert.Contains("<color=#FF0000>", visual);
        Assert.AreEqual(ArabicShaper.ToVisual("أ ب ت ث ج"), Visible(visual));
    }

    /// <summary>A noparse tag's text is text: a "&lt;b&gt;" inside it is printed, not read as a tag.</summary>
    [Test]
    public void ToVisual_NoparseText_IsText()
    {
        string visual = ArabicShaper.ToVisual("ب <noparse>x<b>y</noparse>");
        Assert.IsTrue(visual.Contains("<noparse>") && visual.Contains("</noparse>"), visual);
        StringAssert.Contains("x<b>y", visual, "the literal text kept in its order (Latin)");
    }

    /// <summary>A "&lt;" that opens no tag is text, mirrored with the run it reads in.</summary>
    [Test]
    public void ToVisual_ALoneAngleBracketIsText()
    {
        string visual = ArabicShaper.ToVisual("< ب");
        Assert.AreEqual(3, visual.Length);
        Assert.AreEqual('>', visual[2], "mirrored at the line's left end");
    }

    /// <summary>The shape-once guard: a text already in visual order comes back unchanged, so a label shaped twice reads as shaped once.</summary>
    [TestCase("قبول")]
    [TestCase("الاستقرار: 87%")]
    [TestCase("ب Abbasid Baghdad (Medieval).")]
    [TestCase("<b>صورته واسمه</b> ب")]
    [TestCase("سطر\nسطر آخر 3")]
    public void ToVisual_ShapesOnce(string logical)
    {
        string once = ArabicShaper.ToVisual(logical);
        Assert.AreEqual(once, ArabicShaper.ToVisual(once), "a visual text is not shaped again");
        string[] laid = once.Replace('\u00A0', ' ').Split('\n');
        CollectionAssert.AreEqual(laid, ArabicShaper.ToVisualLines(laid), "nor line by line");
        Assert.IsFalse(ArabicShaper.IsVisual(logical), "logical Arabic is not visual");
        Assert.IsFalse(ArabicShaper.IsVisual("ACCEPT 1"), "Latin is not visual");
        Assert.IsTrue(ArabicShaper.IsVisual(ArabicShaper.ToVisual("قبول")));
    }

    /// <summary>CODE-44: ToLogical undoes ToVisual (a ligature gives its lam and alef, a no-break space a space), so a shaped argument goes back to logical text before its template is shaped.</summary>
    [TestCase("قبول")]
    [TestCase("لا")]
    [TestCase("الأدلة")]
    [TestCase("الاستقرار: 87%")]
    [TestCase("عدم تطابق")]
    [TestCase("(اليوم)")]
    [TestCase("اليوم 3")]
    [TestCase("ب Abbasid Baghdad 12 ت")]
    [TestCase("ب (Rome) ت")]
    [TestCase("<b>صورته واسمه</b> ب")]
    [TestCase("رقم -12 و 3.5 من (ب)")]
    [TestCase("سطر\nسطر آخر 3")]
    public void ToLogical_UndoesToVisual(string logical)
    {
        Assert.AreEqual(logical, ArabicShaper.ToLogical(ArabicShaper.ToVisual(logical)));
    }

    [Test]
    public void ToLogical_LeavesTextWithoutShapedLetters()
    {
        Assert.AreEqual("ACCEPT 1", ArabicShaper.ToLogical("ACCEPT 1"));
        Assert.AreEqual("قبول", ArabicShaper.ToLogical("قبول"), "logical Arabic is already logical");
        Assert.AreEqual(string.Empty, ArabicShaper.ToLogical(null));
    }

    /// <summary>The width of a line as a box would measure it: one unit per visible character.</summary>
    private static float Units(string visual) => Visible(visual).Length;

    /// <summary>CODE-42: wrap on the logical text, then reorder each line: the first line holds the first words read, each line reads right to left by itself, and no word breaks.</summary>
    [Test]
    public void Wrap_BreaksTheLogicalText_ThenReordersEachLine()
    {
        string[] words = { "أول", "ثاني", "ثالث", "رابع", "خامس" };
        List<string> lines = ArabicShaper.Wrap(string.Join(" ", words), 9f, Units);
        CollectionAssert.AreEqual(ArabicShaper.ToVisualLines(new[] { "أول ثاني", "ثالث رابع", "خامس" }), lines, "first words on the first line, each line in visual order");
        Assert.IsTrue(lines.All(l => Units(l) <= 9f), "every line fits");
        Assert.IsFalse(lines.Any(l => l.Contains('\u00A0')), "plain spaces");
        Assert.AreEqual(string.Join(" ", words), string.Join(" ", lines.Select(ArabicShaper.ToLogical)), "nothing lost or reordered between lines");
    }

    /// <summary>A word wider than the line breaks between letters, its first letters (read first) on the first line.</summary>
    [Test]
    public void Wrap_ALongWord_BreaksFirstLettersFirst()
    {
        List<string> lines = ArabicShaper.Wrap("بتثجحخ", 4f, Units);
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual("بتثج", ArabicShaper.ToLogical(lines[0]));
        Assert.AreEqual("بتثجحخ", string.Concat(lines.Select(ArabicShaper.ToLogical)));
    }

    /// <summary>Mixed direction in a wrapped text: numbers and a bracketed Latin label keep their own order on the line they land on.</summary>
    [Test]
    public void Wrap_MixedDirection_KeepsNumbersAndBracketsWhole()
    {
        const string text = "الرقم 1234 من بغداد (Medieval) اليوم";
        List<string> lines = ArabicShaper.Wrap(text, 18f, Units);
        Assert.Greater(lines.Count, 1);
        Assert.IsTrue(lines.Any(l => l.Contains("1234")), "the number reads left to right");
        Assert.IsTrue(lines.Any(l => l.Contains("(Medieval)")), "the bracketed Latin label stays whole");
        Assert.AreEqual(text, string.Join(" ", lines.Select(ArabicShaper.ToLogical)));
    }

    /// <summary>A tag open across a line break is closed at the line's end and opened again on the next line: every line balanced, the style kept.</summary>
    [Test]
    public void Wrap_ATagAcrossLines_IsCarriedAndBalanced()
    {
        List<string> lines = ArabicShaper.Wrap("أ <b>ب ت ث</b> ج", 3f, Units);
        Assert.Greater(lines.Count, 1);
        Assert.IsTrue(lines.All(Balanced), string.Join(" | ", lines));
        Assert.AreEqual("أ ب ت ث ج", string.Join(" ", lines.Select(l => Visible(ArabicShaper.ToLogical(l)))));
        List<string> carried = ArabicShaper.ToVisualLines(new[] { "<b>ب ت", "ث</b> ج" });
        Assert.IsTrue(carried.All(Balanced), string.Join(" | ", carried));
        Assert.IsTrue(carried.All(l => l.Contains("<b>")), "both lines keep the bold");
    }

    [Test]
    public void Wrap_WithoutAWidth_KeepsEachParagraphOnOneLine()
    {
        CollectionAssert.AreEqual(ArabicShaper.ToVisualLines(new[] { "أ ب", "Desk 3" }), ArabicShaper.Wrap("أ ب\nDesk 3", 0f, Units));
        CollectionAssert.AreEqual(new[] { string.Empty }, ArabicShaper.Wrap(null, 10f, Units));
    }

    /// <summary>An RTL body is right-aligned: IsRightToLeft reads the first letter, tags and digits skipped.</summary>
    [TestCase("قبول 3", true)]
    [TestCase("<b>قبول</b>", true)]
    [TestCase("3 قبول", true)]
    [TestCase("Desk قبول", false)]
    [TestCase("", false)]
    public void IsRightToLeft_ReadsTheFirstLetter(string text, bool expected)
    {
        Assert.AreEqual(expected, ArabicShaper.IsRightToLeft(text));
    }
}
