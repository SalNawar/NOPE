using System.Collections.Generic;
using System.Linq;
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
}
