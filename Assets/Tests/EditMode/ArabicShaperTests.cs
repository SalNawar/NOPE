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
}
